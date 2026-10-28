using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GorillaEscape.CameraTracking;
using GorillaEscape.Contracts;
using GorillaEscape.Input;
using GorillaEscape.Input.Fusion;
using GorillaEscape.Gameplay.Input;
using GorillaEscape.Network;
using GorillaEscape.QA;
using JungleParty.Portable;
using UnityEngine;

// Scene-independent owner. All child process and IPC work stays in the audited Phase 0 supervisors.
[DefaultExecutionOrder(-11000)]
public sealed class PortableServiceHost : MonoBehaviour, IPortableModuleStatus
{
    public static PortableServiceHost Instance { get; private set; }
    public bool IsRunning => java != null && (java.State == IpcState.STARTING || java.State == IpcState.CONNECTING || java.State == IpcState.RUNNING);
    public string Status { get; private set; } = "LISTO PARA CONECTAR";
    private string pregeneratedRoomCode;
    public string RoomCode => !string.IsNullOrEmpty(pregeneratedRoomCode) ? pregeneratedRoomCode : (java?.QaConnection?.roomCode ?? java?.Lobby?.Snapshot?.roomCode ?? "");
    public void EnsureRoomCode()
    {
        if (string.IsNullOrEmpty(pregeneratedRoomCode))
        {
            string env = Environment.GetEnvironmentVariable("GORILLA_ROOM_CODE");
            string priorGenerated = Environment.GetEnvironmentVariable("GORILLA_GENERATED_ROOM_CODE");
            if (!string.IsNullOrEmpty(env) && env.Length >= 4 && !string.Equals(env,priorGenerated,StringComparison.OrdinalIgnoreCase))
                pregeneratedRoomCode = env.ToLowerInvariant();
            else
            {
                const string chars = "abcdefghjkmnpqrstuvwxyz23456789";
                var rng = new System.Random();
                char[] code = new char[4];
                do {
                    for (int i = 0; i < 4; i++) code[i] = chars[rng.Next(chars.Length)];
                    pregeneratedRoomCode = new string(code);
                } while (string.Equals(pregeneratedRoomCode,priorGenerated,StringComparison.OrdinalIgnoreCase));
                Environment.SetEnvironmentVariable("GORILLA_GENERATED_ROOM_CODE", pregeneratedRoomCode);
                Environment.SetEnvironmentVariable("GORILLA_ROOM_CODE", pregeneratedRoomCode);
            }
        }
    }
    // Admission secrets never leave the Java-generated QR over authenticated loopback IPC.
    public QaAdmissionWire Admission => java?.State == IpcState.RUNNING ? java.Qa : null;
    public LobbySnapshotWire Lobby => java?.State == IpcState.RUNNING ? java.Lobby.Snapshot : null;
    public string AdmissionMode => java?.State == IpcState.RUNNING ? java.QaConnection?.admissionMode : null;
    public string ConnectionFallback => java?.State == IpcState.RUNNING ? java.QaConnection?.fallbackReason : null;
    public QaAdmissionWire Preparation => java?.State == IpcState.RUNNING ? java.QaHosting?.prepare : null;
    public string FirewallState => java?.QaConnection?.firewallState ?? "CHECKING";
    public bool NeedsNetworkPermission => FirewallState == "NEEDS_PERMISSION" || FirewallState == "INSPECTION_REQUIRED";
    public bool CleanupComplete => !shutdownFailed && (java?.CleanupComplete ?? true) && (vision?.CleanupComplete ?? true);
    public float LastPhonePacketAt { get; private set; } = -10;
    public float LastPosePacketAt { get; private set; } = -10;
    public bool PhoneConnected => java?.State == IpcState.RUNNING && CurrentPhoneConnected()
        && Time.unscaledTime - LastPhonePacketAt < .25f;
    // Camera availability must not depend on a phone or on fusion alignment.
    public bool CameraConnected => vision?.State == "RUNNING" && fusion != null
        && fusion.Cameras.Fresh(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 250, 50);
    public string CameraStatus => vision?.State == "RUNNING" ? "CÁMARA LISTA" : vision?.Error ?? "CÁMARA SIN INICIAR";
    public string PhoneStatus => PhoneConnected ? "CELULAR CONECTADO" : Status;

    JavaProbeSupervisor java;
    VisionSupervisor vision;
    CameraPreviewClient helpPreview;
    Task helpPreviewClosing;
    Texture2D helpPreviewTexture;
    bool helpPreviewRequested;
    public Texture2D CameraHelpTexture => helpPreviewTexture;
    public CameraInputWire CameraHelpFrame => fusion!=null&&fusion.Cameras.Fresh(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),250,50) ? fusion.Cameras.Latest : null;
    public bool ShowCameraHelp => !quitting && JungleGameplayInput.RequiresCamera(gameplay.Game) && CameraConnected
        && (fusion?.Cameras.Latest?.trackingState!="TRACKING" || (PhoneConnected && !gameplay.Ready));
    InputFusion fusion;
    JungleInputAdapter adapter;
    CancellationTokenSource cancellation;
    Task javaRun, visionRun, closing;
    readonly JungleGameplayInput gameplay = new JungleGameplayInput();
    int refreshedFrame=-1;
    long observedSequence=-1,observedEpoch=-1;
    string observedSession,observedDevice;
    float nextInputDiagnostic;
    public bool IsInputReady { get { RefreshInput(); return gameplay.Ready; } }
    public JungleInputStatus InputStatus { get { RefreshInput(); return gameplay.Status; } }
    public string PauseMessage
    {
        get
        {
            RefreshInput();
            if(!PhoneConnected) return "Conecta el teléfono, permite los sensores y mantén abierta la pantalla del control.";
            if(!JungleGameplayInput.RequiresCamera(gameplay.Game)) return "Revisa los sensores del teléfono y mantén abierto el control.";
            if(!CameraConnected) return "La cámara no está enviando imágenes. Revisa su conexión y vuelve a intentarlo.";
            var camera=fusion?.Cameras.Latest;
            if(camera==null||camera.trackingState!="TRACKING") return "La cámara recibe imagen, pero no detecta al jugador. Ponte frente a ella.";
            if(gameplay.Status==JungleInputStatus.Unaligned) return "Esperando que la cámara y el teléfono se sincronicen. Mantén abierto el control.";
            if(gameplay.Status==JungleInputStatus.PartialInput) return "Faltan datos de las manos o del teléfono. Revisa que estén visibles y permite los sensores.";
            if(gameplay.Status==JungleInputStatus.Reacquiring) return "Recuperando el seguimiento. Mantente frente a la cámara y mueve suavemente el teléfono.";
            return "La cámara recibe imagen. Ponte frente a ella; procura que solo haya una persona visible y muestra las manos.";
        }
    }
    public JungleGame Game => gameplay.Game;
    public void SelectGame(JungleGame game,bool force=false)
    {
        if(!force&&gameplay.Game==game)return;
        gameplay.Select(game);
        fusion?.Association.Ready(1);
        refreshedFrame=-1;
    }
    // Explicit player action: discard the former association and require fresh acquisition.
    public bool ReacquireCamera()
    {
        RefreshInput();
        if(quitting||fusion==null||gameplay.Ready||!JungleGameplayInput.RequiresCamera(gameplay.Game)||!CameraConnected)return false;
        fusion.Association.Ready(1);
        gameplay.Select(gameplay.Game);
        LastPosePacketAt=-10;
        refreshedFrame=-1;
        return true;
    }
    public bool TryReadFrame(out JungleFusedFrame frame)
    {
        RefreshInput();frame=gameplay.Ready?gameplay.Current:null;return frame!=null;
    }
    public bool Authorize(JungleAction action,long revision)
    {
        RefreshInput();
        return PhoneConnected&&(!JungleGameplayInput.RequiresCamera(gameplay.Game)||vision?.State=="RUNNING")&&gameplay.Authorize(action,revision,DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }
    bool admissionRequested, replacementRequested, quitting, allowQuit, previousBackground, shutdownFailed;
    string lastConnectionDiagnostic;

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
        EnsureRoomCode();
        previousBackground = Application.runInBackground;
        Application.runInBackground = true;
        Application.wantsToQuit += WantsToQuit;
    }
    public bool StartModule()
    {
        EnsureRoomCode();
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
        if(!Application.isEditor)GorillaEscape.Core.PackagedRuntimeEnvironment.Configure(root,Application.platform==RuntimePlatform.WindowsPlayer);
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
        observedSequence=observedEpoch=-1;observedSession=observedDevice=null;
        LastPhonePacketAt = LastPosePacketAt = -10;
        gameplay.Select(gameplay.Game);
        refreshedFrame=-1;
        closing = null;
        admissionRequested = false; replacementRequested = false;
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
    void Update() { RefreshInput(); UpdateCameraHelp(); }
    void UpdateCameraHelp()
    {
        bool wanted=ShowCameraHelp && vision?.State=="RUNNING";
        if(vision?.State=="RUNNING"&&wanted!=helpPreviewRequested)
        {
            vision.SetQaPreview(wanted);
            helpPreviewRequested=wanted;
        }
        if(!wanted){CloseCameraHelp();return;}
        if(vision.PreviewPort>0&&helpPreview==null&&(helpPreviewClosing==null||helpPreviewClosing.IsCompleted))
        {
            helpPreview=new CameraPreviewClient();
            helpPreview.Start(vision.PreviewPort);
        }
        var bytes=helpPreview?.Take();
        if(bytes==null)return;
        try
        {
            if(helpPreviewTexture==null)helpPreviewTexture=new Texture2D(2,2,TextureFormat.RGB24,false);
            if(!CameraPreviewClient.ValidJpeg(bytes)||!helpPreviewTexture.LoadImage(bytes)||helpPreviewTexture.width>320||helpPreviewTexture.height>240)
            { Destroy(helpPreviewTexture);helpPreviewTexture=null; }
        }
        finally { Array.Clear(bytes,0,bytes.Length); }
    }
    void CloseCameraHelp()
    {
        if(helpPreview!=null){var old=helpPreview;old.Dispose();helpPreviewClosing=old.Run;helpPreview=null;}
        if(helpPreviewTexture!=null){Destroy(helpPreviewTexture);helpPreviewTexture=null;}
    }
    void RefreshInput()
    {
        if(refreshedFrame==Time.frameCount)return;
        refreshedFrame=Time.frameCount;
        long now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if(!quitting&&vision!=null&&fusion!=null)
        {
            vision.Poll();
            if(vision.Take(out var frame,out var received))fusion.ObserveCamera(frame,received);
        }
        if(quitting||java==null||java.State!=IpcState.RUNNING)
        {
            gameplay.Update(null,now);
            LastPhonePacketAt=LastPosePacketAt=-10;
            return;
        }
        var connection=java.QaConnection;
        if(connection!=null){
            string diagnostic=(connection.requestedMode??"?")+"/"+(connection.resolvedMode??"?")+"/"+(connection.bootstrapState??"?")+"/"+(connection.fallbackReason??"?");
            if(diagnostic!=lastConnectionDiagnostic){lastConnectionDiagnostic=diagnostic;Debug.Log("Gorilla connection: "+diagnostic);}
        }
        if(java.Qa?.state=="SCANNED/JOINED" && !replacementRequested && (java.Lobby.Snapshot?.players?.Length??0)<4){java.RegenerateQa();replacementRequested=true;}
        else if(java.Qa?.state=="AVAILABLE")replacementRequested=false;
        if(!admissionRequested){java.RegenerateQa();admissionRequested=true;}
        else if(!PhoneConnected&&java.QaConnection?.resolvedMode=="CLOUDFLARED"&&java.QaConnection?.admissionMode!="CLOUDFLARED")
        {
            java.RegenerateQa();
        }
        java.PhoneInputs.TryLatest(1,out var phone);
        if(phone!=null)
        {
            if(phone.sequence!=observedSequence||phone.connectionEpoch!=observedEpoch||phone.sessionId!=observedSession||phone.deviceSessionId!=observedDevice)
            {
                observedSequence=phone.sequence;observedEpoch=phone.connectionEpoch;observedSession=phone.sessionId;observedDevice=phone.deviceSessionId;
                fusion.ObservePhone(phone);
                if(adapter.TryMotion(phone,now,out _) || (gameplay.Game==JungleGame.LogBalance && adapter.TryTilt(phone,now,out _)))LastPhonePacketAt=Time.unscaledTime;
            }
        }
        var candidate=fusion.EvaluateLatest(1,now);
        if(gameplay.Game==JungleGame.LogBalance)gameplay.UpdatePhone(phone,now);
        else if(vision.State!="RUNNING")gameplay.Update(null,now);
        else gameplay.Update(candidate,now);
        LastPosePacketAt=candidate.Eligible?Time.unscaledTime:-10;
        if(gameplay.Game!=JungleGame.None&&!gameplay.Ready&&Time.unscaledTime>=nextInputDiagnostic)
        {
            nextInputDiagnostic=Time.unscaledTime+3f;
            Debug.Log("Gorilla input: game="+gameplay.Game+" status="+gameplay.Status
                +" phoneFresh="+candidate.PhoneReceiptFresh+" cameraFresh="+candidate.CameraFrameFresh
                +" camera="+(vision?.State??"STOPPED")+" tracking="+(candidate.RawCamera?.trackingState??"NONE")
                +" association="+candidate.LockState+" alignment="+candidate.Status
                +" phoneAgeMs="+(candidate.RawPhone==null?-1:candidate.RawPhoneAge)
                +" cameraAgeMs="+(candidate.RawCamera==null?-1:candidate.RawCameraAge));
        }
        Status=NeedsNetworkPermission?"AUTORIZA EL ACCESO A LA RED LOCAL":PhoneConnected?"CELULAR CONECTADO":"ESPERANDO EL CELULAR";
    }
    bool CurrentPhoneConnected() => java.PhoneInputs.TryLatest(1,out var input) && input.connectionState!="DISCONNECTED";
    public void PresentPhoneGame(GorillaEscape.Contracts.GamePresentationWire context) { if(java?.State==IpcState.RUNNING)java.PresentPhoneGame(context); }
    public void InstallNetwork() { if (java?.State == IpcState.RUNNING && NeedsNetworkPermission) java.RequestNetworkAction("INSTALL"); }
    public void RegenerateAdmission() { if (java?.State == IpcState.RUNNING) java.RegenerateQa(); }
    public Task ShutdownAsync() => closing ?? (closing = Close());
    async Task Close()
    {
        gameplay.Select(gameplay.Game);
        refreshedFrame=-1;
        LastPhonePacketAt = LastPosePacketAt = -10;
        try
        {
            if(helpPreviewRequested&&vision?.State=="RUNNING")vision.SetQaPreview(false);
            helpPreviewRequested=false;CloseCameraHelp();
            if(helpPreviewClosing!=null)await helpPreviewClosing;
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
