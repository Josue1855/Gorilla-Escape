using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using JungleParty.Portable;
using UnityEngine;
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
    public string MotionEndpoint => "http://127.0.0.1:8788/motion?room=";
    public string PoseEndpoint => "http://127.0.0.1:8788/pose";

    private readonly object statusLock = new object();
    private Process preparationProcess;
    private Process cameraProcess;
    private Process tunnelProcess;

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
            tunnelProcess = StartTunnel(cloudflared, Path.GetDirectoryName(cloudflared));
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
        if (!match.Success) return;
        lock (statusLock)
        {
            PublicControllerUrl = match.Value;
            Status = "ENLACE LISTO";
        }
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
        return Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
#else
        return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "PortableModule"));
#endif
    }

    private static string Quote(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";

    public void StopModule()
    {
        StopOwned(tunnelProcess);
        StopOwned(cameraProcess);
        StopOwned(preparationProcess);
        tunnelProcess = cameraProcess = preparationProcess = null;
        IsRunning = false;
        PublicControllerUrl = string.Empty;
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
