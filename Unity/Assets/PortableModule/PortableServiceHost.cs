using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GorillaEscape.CameraTracking;
using GorillaEscape.Contracts;
using GorillaEscape.Input;
using GorillaEscape.Input.Fusion;
using GorillaEscape.Network;
using JungleParty.Portable;
using UnityEngine;

// Scene-independent owner. All child process and IPC work stays in the audited Phase 0 supervisors.
public sealed class PortableServiceHost : MonoBehaviour, IPortableModuleStatus
{
    public static PortableServiceHost Instance { get; private set; }
    public bool IsRunning => java != null && (java.State == IpcState.STARTING || java.State == IpcState.CONNECTING || java.State == IpcState.RUNNING);
    public string Status { get; private set; } = "LISTO PARA CONECTAR";
    // Admission secrets never leave the Java-generated QR over authenticated loopback IPC.
    public string PublicControllerUrl => string.Empty;
    public QaAdmissionWire Admission => java?.State == IpcState.RUNNING ? java.Qa : null;
    public QaAdmissionWire Preparation => java?.State == IpcState.RUNNING ? java.QaHosting?.prepare : null;
    public string FirewallState => java?.QaConnection?.firewallState ?? "CHECKING";
    public bool NeedsNetworkPermission => FirewallState == "NEEDS_PERMISSION" || FirewallState == "INSPECTION_REQUIRED";
    public bool CleanupComplete => !shutdownFailed && (java?.CleanupComplete ?? true) && (vision?.CleanupComplete ?? true);
    public float LastPhonePacketAt { get; private set; } = -10;
    public float LastPosePacketAt { get; private set; } = -10;
    public bool PhoneConnected => java?.State == IpcState.RUNNING && CurrentPhoneConnected()
        && Time.unscaledTime - LastPhonePacketAt < .25f;
    public bool CameraConnected => PhoneConnected && vision?.State == "RUNNING" && Time.unscaledTime - LastPosePacketAt < .25f;
    public string CameraStatus => vision?.State == "RUNNING" ? "CÁMARA LISTA" : vision?.Error ?? "CÁMARA SIN INICIAR";
    public string PhoneStatus => PhoneConnected ? "CELULAR CONECTADO" : Status;

    JavaProbeSupervisor java;
    VisionSupervisor vision;
    InputFusion fusion;
    JungleInputAdapter adapter;
    CancellationTokenSource cancellation;
    Task javaRun, visionRun, closing;
    string motionJson, poseJson;
    bool admissionRequested, quitting, allowQuit, previousBackground, shutdownFailed;

    public static PortableServiceHost Ensure()
    {
        if (Instance != null) return Instance;
        return new GameObject("FASE 0 - Servicios supervisados").AddComponent<PortableServiceHost>();
    }
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        previousBackground = Application.runInBackground;
        Application.runInBackground = true;
        Application.wantsToQuit += WantsToQuit;
    }
    public bool StartModule()
    {
        if (IsRunning) return true;
        if (quitting || closing != null && !closing.IsCompleted || !CleanupComplete) return false;
        // Editor tests and ordinary scene editing never launch physical services implicitly.
        if (Application.isBatchMode || Application.isEditor && !GorillaEscape.Core.ProductScenes.CanStartEditorServices(Application.isBatchMode, Environment.GetEnvironmentVariable("GORILLA_EDITOR_PHYSICAL_QA")))
        {
            Status = "EDITOR: ACTIVA EL INICIO FÍSICO PARA CONECTAR";
            return false;
        }
        if (FindFirstObjectByType<GorillaEscape.QA.QaRuntimeService>() != null)
        {
            Status = "CIERRA LA SESIÓN DE DIAGNÓSTICO ANTES DE ABRIR LOS JUEGOS";
            return false;
        }
        string root = Environment.GetEnvironmentVariable("GORILLA_PACKAGE_ROOT") ?? Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string javaPath = Environment.GetEnvironmentVariable("GORILLA_IPC_JAVA") ?? Path.Combine(root, "Java", "runtime", "bin", Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor ? "java.exe" : "java");
        string jarPath = Environment.GetEnvironmentVariable("GORILLA_IPC_JAR") ?? Path.Combine(root, "Java", "server.jar");
        if (!Path.IsPathRooted(javaPath) || !File.Exists(javaPath) || !Path.IsPathRooted(jarPath) || !File.Exists(jarPath))
        {
            Status = "FALTA EL RUNTIME JAVA O EL SERVIDOR EMPAQUETADO";
            return false;
        }
        java?.Dispose();
        vision?.Dispose();
        cancellation?.Dispose();
        cancellation = new CancellationTokenSource();
        Environment.SetEnvironmentVariable("GORILLA_QA_OVERLAY", "1");
        java = new JavaProbeSupervisor(enableLobby: true);
        vision = new VisionSupervisor();
        fusion = new InputFusion(java.PhoneInputs, new FusionSettings());
        fusion.Association.Ready(1); // This alpha has one turn-active player; never select another player's input.
        adapter = new JungleInputAdapter();
        LastPhonePacketAt = LastPosePacketAt = -10;
        motionJson = poseJson = null;
        closing = null;
        admissionRequested = false;
        Status = "INICIANDO SERVIDOR LOCAL";
        javaRun = RunJava(javaPath, jarPath);
        visionRun = Environment.GetEnvironmentVariable("GORILLA_CAMERA_ENABLED") == "0" ? Task.CompletedTask : RunVision(root);
        return true;
    }
    async Task RunJava(string executable, string jar)
    {
        try { await java.RunLifecycleAsync(executable, jar, cancellation.Token); }
        catch (Exception) { if (!quitting) Status = "SERVIDOR NO DISPONIBLE: " + (java.LastSummary?.errorCode ?? "REVISAR CONFIGURACIÓN"); }
    }
    async Task RunVision(string root)
    {
        try { await vision.StartPackagedAsync(root, cancellation.Token); }
        catch (Exception) { Debug.LogWarning("JUNGLE_CAMERA_UNAVAILABLE (revisa el paquete Vision y el diagnóstico de Fase 0)"); }
    }
    void Update()
    {
        if (quitting || java == null) return;
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (java.State != IpcState.RUNNING)
        {
            motionJson = poseJson = null;
            LastPhonePacketAt = LastPosePacketAt = -10;
            return;
        }
        if (!admissionRequested) { java.RegenerateQa(); admissionRequested = true; }
        Status = NeedsNetworkPermission ? "AUTORIZA EL ACCESO A LA RED LOCAL" : PhoneConnected ? "CELULAR CONECTADO" : "ESPERANDO EL CELULAR";
        if (java.PhoneInputs.TryLatest(1, out var phone))
        {
            fusion.ObservePhone(phone);
            if (adapter.TryMotion(phone, now, out var motion)) { motionJson = JsonUtility.ToJson(motion); LastPhonePacketAt = Time.unscaledTime; }
        }
        if (!PhoneConnected) motionJson = null;
        vision.Poll();
        if (vision.Take(out var frame, out var received)) fusion.ObserveCamera(frame, received);
        var candidate = fusion.Evaluate(1, now);
        if (candidate.Eligible && adapter.TryPose(candidate.Camera, candidate.SubjectId, now, out var pose))
        { poseJson = JsonUtility.ToJson(pose); LastPosePacketAt = Time.unscaledTime; }
        if (!CameraConnected || !candidate.Eligible) { poseJson = null; LastPosePacketAt = -10; }
    }
    bool CurrentPhoneConnected() => java.PhoneInputs.TryLatest(1, out var input) && input.connectionState != "DISCONNECTED";
    public string ConsumeMotion() { var value = motionJson; motionJson = null; return PhoneConnected ? value : null; }
    public string ConsumePose() { var value = poseJson; poseJson = null; return CameraConnected ? value : null; }
    public void InstallNetwork() { if (java?.State == IpcState.RUNNING && NeedsNetworkPermission) java.RequestNetworkAction("INSTALL"); }
    public void RegenerateAdmission() { if (java?.State == IpcState.RUNNING) java.RegenerateQa(); }
    public void ReportReadyState(bool phoneConnected, bool cameraConnected) { }
    public Task ShutdownAsync() => closing ?? (closing = Close());
    async Task Close()
    {
        motionJson = poseJson = null;
        LastPhonePacketAt = LastPosePacketAt = -10;
        try
        {
            cancellation?.Cancel();
            java?.RequestStop();
            if (javaRun != null) await javaRun;
            if (visionRun != null) await visionRun;
            if (vision != null) await vision.StopAsync();
            fusion?.Clear();
            if (CleanupComplete) { java?.Dispose(); vision?.Dispose(); }
        }
        catch (Exception)
        {
            shutdownFailed = true;
            Debug.LogError("JUNGLE_PHASE0_CLEANUP_FAILED");
        }
        Status = CleanupComplete ? "SERVICIOS DETENIDOS" : "CIERRE INCOMPLETO: REVISA EL DIAGNÓSTICO";
        Debug.Log("JUNGLE_PHASE0_CLEANUP complete=" + CleanupComplete);
    }
    public async void RestartModule()
    {
        await ShutdownAsync();
        if (!quitting && CleanupComplete) StartModule();
    }
    public async void StopModule() { await ShutdownAsync(); }
    bool WantsToQuit()
    {
        if (allowQuit) return true;
        if (!quitting) { quitting = true; _ = QuitAfterCleanup(); }
        return false;
    }
    async Task QuitAfterCleanup()
    {
        await ShutdownAsync();
        allowQuit = true;
        Application.Quit(CleanupComplete ? 0 : 2);
    }
    async void OnDestroy()
    {
        if (Instance != this) return;
        quitting = true;
        Application.wantsToQuit -= WantsToQuit;
        await ShutdownAsync();
        cancellation?.Dispose();
        Application.runInBackground = previousBackground;
        Instance = null;
    }
}
