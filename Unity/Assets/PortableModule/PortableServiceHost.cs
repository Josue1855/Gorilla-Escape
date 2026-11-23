using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using JungleParty.Portable;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

public sealed class PortableServiceHost : MonoBehaviour, IPortableModuleStatus
{
    private const string RootEnvironmentVariable = "JUNGLE_PARTY_MODULE_ROOT";
    private static readonly Regex TunnelUrl = new Regex(
        @"https://[a-z0-9-]+\.trycloudflare\.com", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static PortableServiceHost Instance { get; private set; }
    public bool IsRunning { get; private set; }
    public string Status { get; private set; } = "MÓDULO SIN INICIAR";
    public string PublicControllerUrl { get; private set; } = string.Empty;
    public string ControllerUrlForRoom(string room)
    {
        if (string.IsNullOrEmpty(PublicControllerUrl)) return string.Empty;
        return PublicControllerUrl + "/?room=" + Uri.EscapeDataString(room ?? string.Empty);
    }
    public string MotionEndpoint => "http://127.0.0.1:8788/motion?room=";
    public string PoseEndpoint => "http://127.0.0.1:8788/pose";

    private readonly object statusLock = new object();
    private Process preparationProcess;
    private Process cameraProcess;
    private Process tunnelProcess;
    private string tunnelExecutable;
    private string announcedTunnelUrl;
    private bool tunnelRegistered;
    private Coroutine tunnelProbe;
    private Process publicPageProbe;
    private bool waitingForPublicLink;
    private float nextTunnelRetry;

    public static PortableServiceHost Ensure()
    {
        if (Instance != null) return Instance;
        var host = new GameObject("MÓDULO PORTABLE - Servicios locales");
        return host.AddComponent<PortableServiceHost>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool StartModule()
    {
        if (IsRunning) return true;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        string root = ResolveModuleRoot();
        string python = Path.Combine(root, "CameraTracker", ".venv", "Scripts", "python.exe");
        string tracker = Path.Combine(root, "CameraTracker", "camera_tracker.py");
        string cloudflared = Path.Combine(root, "MotionRelay", "cloudflared.exe");
        string preparation = Path.Combine(root, "PREPARAR_MODULO.ps1");

        if (!File.Exists(tracker) || !File.Exists(preparation))
        {
            Status = "FALTAN ARCHIVOS DEL MÓDULO";
            Debug.LogError("No se encontró el módulo portable. Raíz buscada: " + root);
            return false;
        }

        if (!File.Exists(python) || !File.Exists(cloudflared))
        {
            if (preparationProcess != null && !preparationProcess.HasExited) return false;
            try
            {
                preparationProcess = StartHidden(
                    "powershell.exe",
                    "-NoProfile -ExecutionPolicy Bypass -File " + Quote(preparation),
                    root);
                Status = "PREPARANDO MÓDULO · PRIMER INICIO";
            }
            catch (Exception exception)
            {
                Status = "NO SE PUDO PREPARAR EL MÓDULO";
                Debug.LogException(exception);
            }
            return false;
        }

        try
        {
            cameraProcess = StartHidden(python, Quote(tracker), Path.GetDirectoryName(tracker));
            tunnelExecutable = cloudflared;
            tunnelProcess = StartTunnel(tunnelExecutable, Path.GetDirectoryName(tunnelExecutable));
            IsRunning = true;
            Status = "INICIANDO CÁMARA Y CONTROL";
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            StopModule();
            Status = "NO SE PUDO INICIAR EL MÓDULO";
            return false;
        }
#else
        Status = "MÓDULO DISPONIBLE SOLO EN WINDOWS EN ESTE ALPHA";
        return false;
#endif
    }

    private void Update()
    {
        if (IsRunning && tunnelProcess != null && tunnelProcess.HasExited)
        {
            CancelTunnelProbe();
            tunnelProcess.Dispose();
            tunnelProcess = null;
            lock (statusLock)
            {
                announcedTunnelUrl = PublicControllerUrl = string.Empty;
                tunnelRegistered = false;
            }
            nextTunnelRetry = Time.unscaledTime + 3f;
        }
        if (IsRunning && tunnelProcess == null && Time.unscaledTime >= nextTunnelRetry)
        {
            nextTunnelRetry = Time.unscaledTime + 5f;
            try
            {
                tunnelProcess = StartTunnel(tunnelExecutable, Path.GetDirectoryName(tunnelExecutable));
            }
            catch (Exception exception)
            {
                Debug.LogWarning("No se pudo reabrir el túnel: " + exception.Message);
            }
        }

        string candidate;
        lock (statusLock)
            candidate = tunnelRegistered ? announcedTunnelUrl : null;
        if (IsRunning && tunnelProcess != null && tunnelProbe == null &&
            string.IsNullOrEmpty(PublicControllerUrl) && !string.IsNullOrEmpty(candidate))
        {
            waitingForPublicLink = true;
            tunnelProbe = StartCoroutine(VerifyPublicLink(candidate, tunnelProcess));
        }

        if (preparationProcess == null || !preparationProcess.HasExited) return;
        int exitCode = preparationProcess.ExitCode;
        preparationProcess.Dispose();
        preparationProcess = null;
        if (exitCode == 0) StartModule();
        else Status = "LA PREPARACIÓN FALLÓ · REVISA TU INTERNET";
    }

    public void ReportReadyState(bool phoneConnected, bool cameraConnected)
    {
        if (!IsRunning) return;
        lock (statusLock)
        {
            if (phoneConnected && cameraConnected) Status = "MÓDULO LISTO";
            else if (tunnelProcess == null) Status = "RECONECTANDO TÚNEL";
            else if (waitingForPublicLink) Status = "COMPROBANDO EL ENLACE";
            else if (string.IsNullOrEmpty(PublicControllerUrl)) Status = "CREANDO ENLACE DEL CONTROL";
            else if (!cameraConnected) Status = "ESPERANDO CÁMARA";
            else Status = "ESPERANDO CELULAR";
        }
    }

    private Process StartTunnel(string executable, string workingDirectory)
    {
        var info = HiddenInfo(executable, "tunnel --no-autoupdate --protocol http2 --edge-ip-version 4 --url http://127.0.0.1:8788", workingDirectory);
        info.RedirectStandardOutput = true;
        info.RedirectStandardError = true;
        var process = new Process { StartInfo = info, EnableRaisingEvents = true };
        process.OutputDataReceived += ReadTunnelLine;
        process.ErrorDataReceived += ReadTunnelLine;
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }

    private void ReadTunnelLine(object sender, DataReceivedEventArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Data)) return;
        Match match = TunnelUrl.Match(args.Data);
        lock (statusLock)
        {
            if (match.Success) announcedTunnelUrl = match.Value;
            if (args.Data.Contains("Registered tunnel connection")) tunnelRegistered = true;

        }
    }

    [Serializable]
    private sealed class DnsReply
    {
        public int Status;
        public DnsAnswer[] Answer;
    }

    [Serializable]
    private sealed class DnsAnswer
    {
        public int type;
        public string data;
    }

    private IEnumerator VerifyPublicLink(string candidate, Process process)
    {
        string host = new Uri(candidate).Host;
        float deadline = Time.realtimeSinceStartup + 100f;
        while (Time.realtimeSinceStartup < deadline && process == tunnelProcess && !process.HasExited)
        {
            // Avoid querying the local DNS while the new hostname may still be unpublished.
            string ip = null;
            using (var dns = UnityWebRequest.Get("https://dns.google/resolve?type=A&name=" + UnityWebRequest.EscapeURL(host)))
            {
                dns.timeout = 4;
                yield return dns.SendWebRequest();
                if (dns.result == UnityWebRequest.Result.Success)
                {
                    var answer = JsonUtility.FromJson<DnsReply>(dns.downloadHandler.text);
                    var record = answer?.Status == 0 && answer.Answer != null
                        ? Array.Find(answer.Answer, item => item.type == 1 && System.Net.IPAddress.TryParse(item.data, out _))
                        : null;
                    ip = record?.data;
                }

                if (dns.result == UnityWebRequest.Result.Success && string.IsNullOrEmpty(ip))
                {
                    yield return new WaitForSecondsRealtime(2f);
                    continue;
                }
            }

            string page = null;
            string curl = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "curl.exe");
            if (ip != null && File.Exists(curl))
            {
                // Preserve HTTPS hostname and certificate validation while bypassing stale local DNS.
                var info = HiddenInfo(curl, "--silent --fail --max-time 5 --resolve " +
                    Quote(host + ":443:" + ip) + " " + Quote(candidate + "/?check=unity"), Path.GetDirectoryName(curl));
                info.RedirectStandardOutput = true;
                try { publicPageProbe = Process.Start(info); }
                catch (Exception exception) { Debug.LogWarning("No se pudo iniciar la comprobación HTTPS: " + exception.Message); }
                if (publicPageProbe != null)
                {
                    var output = publicPageProbe.StandardOutput.ReadToEndAsync();
                    while (!publicPageProbe.HasExited || !output.IsCompleted) yield return null;
                    if (publicPageProbe.ExitCode == 0 && output.Status == System.Threading.Tasks.TaskStatus.RanToCompletion)
                        page = output.Result;
                    publicPageProbe.Dispose();
                    publicPageProbe = null;
                }
            }
            else
            {
                using var request = UnityWebRequest.Get(candidate + "/?check=unity");
                request.timeout = 5;
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success) page = request.downloadHandler.text;
            }

            if (page != null && page.Contains("id=\"activate\""))
            {
                PublicControllerUrl = candidate;
                break;
            }
            yield return new WaitForSecondsRealtime(2f);
        }

        tunnelProbe = null;
        waitingForPublicLink = false;
        if (string.IsNullOrEmpty(PublicControllerUrl) && process == tunnelProcess)
        {
            Debug.LogWarning("El túnel se registró, pero no se pudo abrir la página del control. Se reintentará.");
            StopOwned(tunnelProcess);
            tunnelProcess = null;
            lock (statusLock)
            {
                announcedTunnelUrl = string.Empty;
                tunnelRegistered = false;
            }
            nextTunnelRetry = Time.unscaledTime + 3f;
        }
    }

    private void CancelTunnelProbe()
    {
        if (tunnelProbe != null) StopCoroutine(tunnelProbe);
        StopOwned(publicPageProbe);
        publicPageProbe = null;
        tunnelProbe = null;
        waitingForPublicLink = false;
    }

    private static Process StartHidden(string executable, string arguments, string workingDirectory)
    {
        return Process.Start(HiddenInfo(executable, arguments, workingDirectory));
    }

    private static ProcessStartInfo HiddenInfo(string executable, string arguments, string workingDirectory)
    {
        return new ProcessStartInfo
        {
            FileName = executable,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
    }

    private static string ResolveModuleRoot()
    {
        string configured = Environment.GetEnvironmentVariable(RootEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configured)) return Path.GetFullPath(configured);
#if UNITY_EDITOR
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (File.Exists(Path.Combine(projectRoot, "CameraTracker", "camera_tracker.py")))
            return projectRoot;

        // Compatibilidad temporal con la antigua distribución Prototypes/JosueAlpha/Unity.
        return Path.GetFullPath(Path.Combine(projectRoot, ".."));
#else
        return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "PortableModule"));
#endif
    }

    private static string Quote(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";

    public void StopModule()
    {
        CancelTunnelProbe();
        StopOwned(tunnelProcess);
        StopOwned(cameraProcess);
        StopOwned(preparationProcess);
        tunnelProcess = cameraProcess = preparationProcess = null;
        IsRunning = false;
        lock (statusLock)
        {
            announcedTunnelUrl = PublicControllerUrl = string.Empty;
            tunnelRegistered = false;
        }
        Status = "MÓDULO DETENIDO";
    }

    private static void StopOwned(Process process)
    {
        if (process == null) return;
        try { if (!process.HasExited) process.Kill(); }
        catch (Exception exception) { Debug.LogWarning("No se pudo cerrar un proceso del módulo: " + exception.Message); }
        finally { process.Dispose(); }
    }

    private void OnApplicationQuit() => StopModule();

    private void OnDestroy()
    {
        if (Instance != this) return;
        StopModule();
        Instance = null;
    }
}

