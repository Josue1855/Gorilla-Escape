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
    public bool CameraOnly {get;private set;}
    public bool CameraLossGraceAllowed => (CameraOnly||PhoneConnected)&&JungleGameplayInput.RequiresCamera(gameplay.Game)
        &&(fusion?.Cameras.Latest?.subjects==null||fusion.Cameras.Latest.subjects.Length<=1)
        &&(fusion?.Cameras.Latest?.subjects==null||fusion.Cameras.Latest.subjects.Length==0||!fusion.Cameras.Latest.subjects[0].ambiguous);
    public void SetCameraOnly(bool enabled){CameraOnly=enabled;SelectGame(gameplay.Game,true);}
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
    public bool TryPhoneVisualEstimate(out PhoneVisualEstimate estimate)
    {
        estimate=null;
        return PhoneConnected&&fusion!=null&&fusion.TryPhoneVisualEstimate(1,DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),out estimate);
    }
    // Camera availability must not depend on a phone or on fusion alignment.
    public bool CameraConnected => vision?.State == "RUNNING" && fusion != null
        && fusion.Cameras.Fresh(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 250, 50);
    public string CameraStatus => vision?.State == "RUNNING" ? "CÁMARA LISTA" : vision?.Error ?? "CÁMARA SIN INICIAR";
    public string PhoneStatus => PhoneConnected ? "CELULAR CONECTADO" : Status;

    JavaProbeSupervisor java;
    VisionSupervisor vision;
    CameraPreviewClient helpPreview;
    Task helpPreviewClosing;
    float helpPreviewStartedAt=-1,helpPreviewLastFrameAt=-1,nextPreviewRetry;
    Texture2D helpPreviewTexture;
    bool helpPreviewRequested, cameraSetupVisible;
    bool preferLeftCursor;
    bool cursorHandChosen;
    int cursorSubject=-1,cursorHandSwitchFrame=-1;
    long cursorSequence=-1;
    readonly HandVisualPrediction menuCursor=new HandVisualPrediction();
    public bool PipEnabled { get; set; } = true;
    public void SetCameraSetupVisible(bool visible) => cameraSetupVisible=visible;
    public bool CameraActive => vision?.State=="RUNNING"||vision?.State=="STARTING";
    public string CameraGuidance => GorillaEscape.CameraTracking.CameraGuidance.Describe(CameraConnected,CameraHelpFrame,true);
    public CameraFramingReport CameraFraming => ShotMechanics.EvaluateFraming(CameraConnected,CameraHelpFrame,
        gameplay.Game==JungleGame.Boxing||gameplay.Game==JungleGame.Drums||gameplay.Game==JungleGame.CoconutSlice);
    public Texture2D CameraHelpTexture => helpPreviewTexture;
    public CameraInputWire CameraHelpFrame => fusion!=null&&fusion.Cameras.Fresh(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),250,50) ? fusion.Cameras.Latest : null;
    public bool TryCameraVisualLandmark(int subject,string name,out float x,out float y)
    {
        x=y=0;
        return vision?.State=="RUNNING"&&fusion!=null&&fusion.Cameras.TryVisualLandmark(subject,name,DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),out x,out y);
    }
    public bool TryCameraCursor(out Vector2 screenNormalized, out string handLabel, out bool handRaised)
    {
        screenNormalized = new Vector2(.5f, .5f);
        handLabel = "DER";
        var framing = CameraFraming;
        handRaised = framing.HandRaised;
        var frame = CameraHelpFrame;
        if (!CameraConnected || frame?.subjects == null || frame.subjects.Length != 1 || frame.subjects[0] == null || frame.subjects[0].ambiguous || frame.subjects[0].landmarks == null)
        { menuCursor.Reset();cursorSequence=-1;cursorSubject=-1;cursorHandChosen=false;return false; }
        var subject = frame.subjects[0];
        CameraLandmark left = null, right = null;
        for (int i = 0; i < subject.landmarks.Length; i++)
        {
            var lm = subject.landmarks[i];
            if (lm == null) continue;
            if (lm.name == "left-wrist") left = lm;
            else if (lm.name == "right-wrist") right = lm;
        }
        bool leftOk = left != null && left.visible && left.visibility >= .6f && left.presence >= .6f && left.x >= 0f && left.x <= 1f && left.y >= 0f && left.y <= 1f;
        bool rightOk = right != null && right.visible && right.visibility >= .6f && right.presence >= .6f && right.x >= 0f && right.x <= 1f && right.y >= 0f && right.y <= 1f;
        if (!leftOk && !rightOk) {menuCursor.Reset();cursorSequence=-1;cursorHandChosen=false;return false;}
        if(cursorSubject!=subject.subjectId){menuCursor.Reset();cursorSequence=-1;cursorSubject=subject.subjectId;cursorHandChosen=false;}
        if(!cursorHandChosen){preferLeftCursor=leftOk&&(!rightOk||left.y<right.y-.07f);cursorHandChosen=true;}
        else if(preferLeftCursor&&!leftOk||!preferLeftCursor&&!rightOk)
        {preferLeftCursor=leftOk;menuCursor.Reset();cursorSequence=-1;cursorHandSwitchFrame=Time.frameCount;}
        if(cursorHandSwitchFrame==Time.frameCount)return false;
        bool useLeft = preferLeftCursor;
        var chosen = useLeft ? left : right;
        if(frame.frameSequence!=cursorSequence){menuCursor.Observe(1f-chosen.x,chosen.y,0,0,Time.unscaledTime);cursorSequence=frame.frameSequence;}
        if(!menuCursor.TryPosition(Time.unscaledTime,out float x,out float y))return false;
        HandCursorMapping.Map(x,y,out float screenX,out float screenY);
        screenNormalized = new Vector2(screenX,screenY);
        handLabel = useLeft ? "MANO IZQ" : "MANO DER";
        return true;
    }
    public bool ShowCameraHelp => !quitting && JungleGameplayInput.RequiresCamera(gameplay.Game) && CameraConnected
        && (fusion?.Cameras.Latest?.trackingState!="TRACKING" || ((CameraOnly||PhoneConnected) && !gameplay.Ready));
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
            if(CameraOnly&&!JungleGameplayInput.SupportsCameraOnly(gameplay.Game))return "Este juego necesita un celular. Vuelve al menú de juegos sin celular.";
            if(!CameraOnly&&!PhoneConnected) return "Conecta el teléfono, permite los sensores y mantén abierta la pantalla del control.";
            if(!JungleGameplayInput.RequiresCamera(gameplay.Game)) return "Revisa los sensores del teléfono y mantén abierto el control.";
            if(!CameraConnected) return "La cámara no está enviando imágenes. Revisa su conexión y vuelve a intentarlo.";
            var camera=fusion?.Cameras.Latest;
            string guidance=GorillaEscape.CameraTracking.CameraGuidance.Describe(CameraConnected,camera,gameplay.Game==JungleGame.Boxing||gameplay.Game==JungleGame.Drums||gameplay.Game==JungleGame.CoconutSlice,gameplay.Game==JungleGame.Bowling?(gameplay.LeftHanded?"left":"right"):null);
            if(gameplay.Game==JungleGame.Bowling&&gameplay.Status!=JungleInputStatus.Unaligned&&gameplay.Status!=JungleInputStatus.Reacquiring)return guidance;
            if(camera==null||camera.trackingState!="TRACKING"||guidance!="La cámara detecta tus hombros y manos. Mantente dentro de la imagen.")return guidance;
            if(gameplay.Status==JungleInputStatus.Unaligned) return "La cámara y el teléfono envían datos con tiempos diferentes. Mantén abierto el control y revisa la conexión.";
            if(gameplay.Status==JungleInputStatus.PartialInput) return "Faltan datos de las manos o del teléfono. Revisa que estén visibles y permite los sensores.";
            if(gameplay.Status==JungleInputStatus.Reacquiring) return "Recuperando el seguimiento. Mantente frente a la cámara y mueve suavemente el teléfono.";
            return "La cámara recibe imagen. Ponte frente a ella; procura que solo haya una persona visible y muestra las manos.";
        }
    }
    public JungleGame Game => gameplay.Game;
    public bool LeftHanded => gameplay.LeftHanded;
    public void SetDominantHand(bool left){gameplay.LeftHanded=left;SelectGame(gameplay.Game,true);}
    public bool TakeUiCommand(out MobileUiCommandWire command){command=null;return java?.State==IpcState.RUNNING&&java.TakeUiCommand(out command);}
    public void SelectGame(JungleGame game,bool force=false)
    {
        if(!force&&gameplay.Game==game)return;
        gameplay.Select(game,CameraOnly);
        fusion?.Association.Ready(1);
        refreshedFrame=-1;
    }
    // Explicit player action: discard the former association and require fresh acquisition.
    public bool ReacquireCamera()
    {
        RefreshInput();
        if(quitting||fusion==null||gameplay.Ready||!JungleGameplayInput.RequiresCamera(gameplay.Game)||!CameraConnected)return false;
        fusion.Association.Ready(1);
        gameplay.Select(gameplay.Game,CameraOnly);
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
        return (CameraOnly||PhoneConnected)&&(!JungleGameplayInput.RequiresCamera(gameplay.Game)||vision?.State=="RUNNING")&&gameplay.Authorize(action,revision,DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
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
        if (FindAnyObjectByType<GorillaEscape.QA.QaRuntimeService>() != null)
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
        gameplay.Select(gameplay.Game,CameraOnly);
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
        catch (Exception) { Debug.LogWarning("JUNGLE_CAMERA_UNAVAILABLE " + (vision.Error ?? "UNKNOWN")); }
    }
    void Update() { RefreshInput(); UpdateCameraHelp(); }
    void UpdateCameraHelp()
    {
        bool wanted=(cameraSetupVisible||ShowCameraHelp||(PipEnabled&&(CameraOnly||gameplay.Game==JungleGame.None||JungleGameplayInput.RequiresCamera(gameplay.Game)))) && vision?.State=="RUNNING";
        if(vision?.State=="RUNNING"&&wanted!=helpPreviewRequested)
        {
            vision.SetQaPreview(wanted);
            helpPreviewRequested=wanted;
        }
        if(!wanted){CloseCameraHelp();return;}
        // A dead or stalled preview client must not leave its last JPEG frozen on screen.
        // Retrying only the preview keeps camera tracking and the phone session untouched.
        if(helpPreview!=null&&(helpPreview.State=="FAILED"||
            Time.unscaledTime-(helpPreviewLastFrameAt>=0?helpPreviewLastFrameAt:helpPreviewStartedAt)>3f))
        {
            CloseCameraHelp();nextPreviewRetry=Time.unscaledTime+1f;
        }
        if(vision.PreviewPort>0&&helpPreview==null&&Time.unscaledTime>=nextPreviewRetry&&(helpPreviewClosing==null||helpPreviewClosing.IsCompleted))
        {
            helpPreview=new CameraPreviewClient();
            helpPreviewStartedAt=Time.unscaledTime;helpPreviewLastFrameAt=-1;
            helpPreview.Start(vision.PreviewPort);
        }
        var bytes=helpPreview?.Take();
        if(bytes==null)return;
        try
        {
            if(helpPreviewTexture==null)helpPreviewTexture=new Texture2D(2,2,TextureFormat.RGB24,false);
            if(!CameraPreviewClient.ValidJpeg(bytes)||!helpPreviewTexture.LoadImage(bytes)||helpPreviewTexture.width>320||helpPreviewTexture.height>240)
            { Destroy(helpPreviewTexture);helpPreviewTexture=null; }
            else helpPreviewLastFrameAt=Time.unscaledTime;
        }
        finally { Array.Clear(bytes,0,bytes.Length); }
    }
    void CloseCameraHelp()
    {
        if(helpPreview!=null){var old=helpPreview;old.Dispose();helpPreviewClosing=old.Run;helpPreview=null;}
        if(helpPreviewTexture!=null){Destroy(helpPreviewTexture);helpPreviewTexture=null;}
        helpPreviewStartedAt=helpPreviewLastFrameAt=-1;
    }
    void RefreshInput()
    {
        if(refreshedFrame==Time.frameCount)return;
        refreshedFrame=Time.frameCount;
        long now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if(!quitting&&vision!=null&&fusion!=null)
        {
            vision.Poll();
            if(vision.Take(out var frame,out var received))fusion.ObserveCamera(frame,received,(CameraOnly&&gameplay.Game==JungleGame.CoconutSlice)||gameplay.Game==JungleGame.None||gameplay.Game==JungleGame.Bowling);
        }
        if(CameraOnly&&!quitting&&gameplay.Game!=JungleGame.None)
        {
            gameplay.UpdateCamera(vision?.State=="RUNNING"?fusion?.Cameras.Latest:null,
                fusion?.Association.State??PlayerLockState.UNASSIGNED,fusion?.Association.SubjectId??0,now);
            LastPosePacketAt=gameplay.Ready?Time.unscaledTime:-10;
            return;
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
                +" alignmentDeltaMs="+(candidate.AlignmentDelta==long.MaxValue?-1:candidate.AlignmentDelta)
                +" phoneAgeMs="+(candidate.RawPhone==null?-1:candidate.RawPhoneAge)
                +" cameraAgeMs="+(candidate.RawCamera==null?-1:candidate.RawCameraAge)
                +" cameraProcessingMs="+(candidate.RawCamera==null?-1:Math.Max(0,candidate.RawCamera.processTimestamp-candidate.RawCamera.captureTimestamp)));
        }
        Status=NeedsNetworkPermission?"AUTORIZA EL ACCESO A LA RED LOCAL":PhoneConnected?"CELULAR CONECTADO":"ESPERANDO EL CELULAR";
    }
    bool CurrentPhoneConnected() => java.PhoneInputs.TryLatest(1,out var input) && input.connectionState!="DISCONNECTED";
    public void PresentPhoneGame(GorillaEscape.Contracts.GamePresentationWire context) { if(java?.State==IpcState.RUNNING)java.PresentPhoneGame(context); }
    public void InstallNetwork() { if (java?.State == IpcState.RUNNING && NeedsNetworkPermission) java.RequestNetworkAction("INSTALL"); }
    public void RegenerateAdmission() { if (java?.State == IpcState.RUNNING) java.RegenerateQa(); }
    public void RequestConnectionMode(string mode) { if (java?.State == IpcState.RUNNING) java.RequestConnectionMode(mode); }
    public Task ShutdownAsync() => closing ?? (closing = Close());
    async Task Close()
    {
        gameplay.Select(gameplay.Game,CameraOnly);
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
