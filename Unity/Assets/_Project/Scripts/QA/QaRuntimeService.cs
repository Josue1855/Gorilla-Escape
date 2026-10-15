using System;
using System.Collections;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GorillaEscape.CameraTracking;
using GorillaEscape.Contracts;
using GorillaEscape.Input.Fusion;
using GorillaEscape.Network;
using UnityEngine;
namespace GorillaEscape.QA {
    /// <summary>Bootstrap-owned technical adapters. Product navigation never owns session or capture authority.</summary>
    public sealed class QaRuntimeService : MonoBehaviour {
        internal JavaProbeSupervisor java;
        internal VisionSupervisor vision;
        internal CameraPreviewClient preview;
        internal InputFusion fusion;
        internal readonly CameraInputStore cameras=new CameraInputStore();
        internal readonly LiveQaModel model=new LiveQaModel();
        CancellationTokenSource cancel;
        Task javaRun,visionRun,closing;
        internal Texture2D cameraTexture,qrTexture;
        FrameTimingWindow frameTiming;
        double measureStart,measureEnd,previousFrame;
        bool timingLogged;
        internal int selected=1;
        bool quitting,allowQuit,generated,priorBackground;
        internal string qrState="STARTING",error="";
        internal long qrVisible,joinEnd,applied,presented;
        long sequence=-1,epoch=-1;
        internal int appliedFrame,presentedFrame;
        internal PhoneInputWire sample;
        readonly System.Collections.Generic.HashSet<string> priorPlayers=new System.Collections.Generic.HashSet<string>();
        internal QaMetricsWire baseline=new QaMetricsWire();
        double fpsStart;
        int fpsFrames;
        internal double fps=-1;
        internal string joinClass="NOT CONFIRMED",exportMessage="";
        internal bool physicalConfirmed;
        bool qaPhoneObserved,previewRequested;
        // Editor navigation deliberately has no physical services, even with inherited launcher flags.
        public bool ServicesEnabled => !Application.isEditor;
        public string JavaStatus => !ServicesEnabled ? "JAVA NOT RUNNING (Editor navigation only)" : "JAVA " + java?.State;
        public string CameraStatus => !ServicesEnabled ? "CAMERA NOT RUNNING (Editor navigation only)" : null;
        public bool CleanupComplete => (java?.CleanupComplete ?? true) && (vision?.CleanupComplete ?? true);
        public Task ShutdownAsync()
        {
            quitting = true;
            cancel?.Cancel();
            StopAllCoroutines();
            TestsActive = false;
            return closing ?? (closing = Close());
        }
        void Start(){priorBackground=Application.runInBackground;Application.runInBackground=true;cancel=new CancellationTokenSource();java=new JavaProbeSupervisor(enableLobby:true);if(Environment.GetEnvironmentVariable("GORILLA_MOBILE_LAB")=="1")java.HttpReady+=port=>Debug.Log("QA_LAB_HTTP_READY "+port);vision=new VisionSupervisor();fusion=new InputFusion(java.PhoneInputs,new FusionSettings());Application.wantsToQuit+=WantsToQuit;if(ServicesEnabled){javaRun=StartJava();visionRun=StartVision();}
            else {qrState="QR NOT AVAILABLE";error="Editor navigation only; use run-qa.sh for physical QA";}fpsStart=Time.realtimeSinceStartupAsDouble;
            if(Environment.GetEnvironmentVariable("GORILLA_T018_MEASURE")=="1"){frameTiming=new FrameTimingWindow();previousFrame=fpsStart;measureStart=fpsStart+double.Parse(Environment.GetEnvironmentVariable("GORILLA_CAMERA_WARMUP")??"5");measureEnd=measureStart+double.Parse(Environment.GetEnvironmentVariable("GORILLA_T018_DURATION")??"61");}
            }
        async Task StartJava(){try{await java.RunLifecycleAsync(Environment.GetEnvironmentVariable("GORILLA_IPC_JAVA"),Environment.GetEnvironmentVariable("GORILLA_IPC_JAR"),cancel.Token);}catch(Exception){error="BACKEND FAILED: "+(java.LastSummary?.errorCode??"CHECK CONFIGURATION");}}
        async Task StartVision(){try{await vision.StartPackagedAsync(Path.GetFullPath(Path.Combine(Application.dataPath,"..")),cancel.Token);Debug.Log("CAMERA_LAB_READY");}catch(Exception){error="CAMERA FAILED: "+vision.Error;}}
        void Update(){if(quitting){if(closing==null)closing=Close();if(closing.IsCompleted){allowQuit=true;
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying=false;
#else
                Application.Quit(CleanupComplete?0:2);
#endif
            }return;}
            if(frameTiming!=null&&!timingLogged){double now=Time.realtimeSinceStartupAsDouble;if(previousFrame>=measureStart&&now<=measureEnd)frameTiming.Add((now-previousFrame)*1000);previousFrame=now;if(now>=measureEnd){timingLogged=true;var report=frameTiming.Report();report.clock="Unity Time.realtimeSinceStartupAsDouble; process-local monotonic seconds -> ms";Debug.Log("T018_FRAME_TIMING "+JsonUtility.ToJson(report));}}
            fpsFrames++;double elapsed=Time.realtimeSinceStartupAsDouble-fpsStart;if(elapsed>=1){fps=fpsFrames/elapsed;fpsFrames=0;fpsStart=Time.realtimeSinceStartupAsDouble;}
            vision.Poll();if(vision.State=="RUNNING"&&TestsActive!=previewRequested){vision.SetQaPreview(TestsActive);previewRequested=TestsActive;}if(TestsActive&&vision.PreviewPort>0&&preview==null&&(previewClosing==null||previewClosing.IsCompleted)){preview=new CameraPreviewClient();preview.Start(vision.PreviewPort);}
            if(PhoneMetrics!=null)qaPhoneObserved=true;
            var bytes=preview?.Take();if(bytes!=null){try{if(cameraTexture==null)cameraTexture=new Texture2D(2,2,TextureFormat.RGB24,false);if(!CameraPreviewClient.ValidJpeg(bytes)||!cameraTexture.LoadImage(bytes)||cameraTexture.width>320||cameraTexture.height>240){Destroy(cameraTexture);cameraTexture=null;error="PREVIEW INVALID";}}finally{Array.Clear(bytes,0,bytes.Length);}}
            if(vision.Take(out var frame,out var at)&&cameras.Accept(frame,at))fusion.ObserveCamera(frame,at);
            if(cameras.Latest!=null&&!cameras.Fresh(Now(),250,20))fusion.Association.MarkLost();
            if(TestsActive&&java.State==IpcState.RUNNING&&!generated){generated=true;PrepareQr();java.RegenerateQa();}
            var qr=java.Qa;if(TestsActive&&qr!=null){qrState=qr.state;if(!string.IsNullOrEmpty(qr.modules)){if(qrTexture==null){try{qrTexture=DecodeQr(qr);qrVisible=0;joinEnd=0;joinClass=model.DeviceState;}catch(Exception){error="QR INVALID";}}qr.modules=null;}if(qr.state!="AVAILABLE")ClearQr();}
            if(TestsActive&&(java.State==IpcState.FAILED||java.State==IpcState.STOPPED)){ClearQr();qrState="BACKEND UNAVAILABLE";}
            var lobby=java.Lobby.Snapshot;if(TestsActive&&qrVisible>0&&joinEnd==0&&lobby?.players!=null)foreach(var p in lobby.players)if(!priorPlayers.Contains(PlayerKey(p))&&p.networkReady&&p.inputReady&&p.connectionEpoch==1){joinEnd=Now();break;}
            if(TestsActive&&java.PhoneInputs.TryLatest(selected,out var pInput)){sample=pInput;fusion.ObservePhone(pInput);if(pInput.sequence!=sequence||pInput.connectionEpoch!=epoch){sequence=pInput.sequence;epoch=pInput.connectionEpoch;applied=Now();appliedFrame=Time.frameCount;presented=0;}}
            else {sample=null;applied=presented=0;}
        }
        IEnumerator Presentation(){while(true){yield return new WaitForEndOfFrame();if(TestsActive&&applied>0&&presented==0&&appliedFrame<=renderedFrame){presented=Now();presentedFrame=Time.frameCount;}}}
        public static Texture2D DecodeQr(QaAdmissionWire q){if(q.size<21||q.size>89)throw new InvalidDataException("QR_BOUND");var data=Convert.FromBase64String(q.modules);if(data.Length!=(q.size*q.size+7)/8)throw new InvalidDataException("QR_BOUND");var texture=new Texture2D(q.size,q.size,TextureFormat.RGB24,false){filterMode=FilterMode.Point};var pixels=new Color32[q.size*q.size];for(int y=0;y<q.size;y++)for(int x=0;x<q.size;x++){int i=y*q.size+x;pixels[(q.size-1-y)*q.size+x]=(data[i/8]&(1<<(7-i%8)))!=0?new Color32(0,0,0,255):new Color32(255,255,255,255);}texture.SetPixels32(pixels);texture.Apply();Array.Clear(data,0,data.Length);Array.Clear(pixels,0,pixels.Length);return texture;}
        static string PlayerKey(LobbyPlayerWire player)=>player.playerId+"|"+player.deviceSessionId;
        void PrepareQr(){priorPlayers.Clear();var lobby=java.Lobby.Snapshot;if(lobby!=null)foreach(var p in lobby.players)priorPlayers.Add(PlayerKey(p));}
        void ClearQr(){if(qrTexture!=null){Destroy(qrTexture);qrTexture=null;}}
        static long Now()=>DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        static long Stamp(long n)=>n;
        internal string QrLifecycle {
            get {
                if(qrState=="AVAILABLE")return "QR AVAILABLE / WAITING FOR PLAYER";
                if(qrState=="EXPIRED")return "QR EXPIRED";
                if(qrState=="SESSION_FULL")return "SESSION FULL";
                if(qrState=="SCANNED/JOINED") {
                    var lobby=java.Lobby.Snapshot;
                    if(lobby?.players!=null&&Array.Exists(lobby.players,p=>!priorPlayers.Contains(PlayerKey(p))&&p.networkReady))return "PLAYER CONNECTED";
                    return "PLAYER JOINING";
                }
                return qrState;
            }
        }
        internal QaPhoneMetricsWire PhoneMetrics {
            get {var metrics=java?.QaPhones;return metrics==null?null:Array.Find(metrics,m=>m!=null&&m.playerId==selected&&m.connectionEpoch==sample?.connectionEpoch&&Now()-m.receivedAt>=0&&Now()-m.receivedAt<2500);}
        }
        public void SetDeviceState(string state){model.DeviceState=state;if(joinEnd==0)joinClass=state;}
        public bool MeasuringFrames => frameTiming!=null&&!timingLogged;
        public bool TestsActive {get; private set;}
        internal int renderedFrame;
        Task previewClosing;
        public void EnterTests(){TestsActive=true;generated=false;StartCoroutine(Presentation());Debug.Log("QA_CENTER_OPEN");}
        public void LeaveTests(){
            TestsActive=false;if(previewRequested){vision.SetQaPreview(false);previewRequested=false;}model.Stop(Now());Debug.Log("QA_CENTER_CLOSED");StopAllCoroutines();
            if(preview!=null){var old=preview;old.Dispose();previewClosing=old.Run;preview=null;}
            if(cameraTexture!=null){Destroy(cameraTexture);cameraTexture=null;}
            ClearQr();sample=null;sequence=epoch=-1;applied=presented=qrVisible=joinEnd=0;generated=false;
        }
        public void RegenerateQr(){if(java.State!=IpcState.RUNNING)return;ClearQr();qrVisible=joinEnd=0;PrepareQr();java.RegenerateQa();}
        public void ResetMetrics(){baseline=java.QaMetrics??new QaMetricsWire();model.Reset();qrVisible=joinEnd=0;exportMessage="QA aggregates reset; session retained";}
        public void SelectPlayer(int player){selected=player;sample=null;sequence=epoch=-1;applied=presented=0;}
        public void StartTest(){model.Start(Now());exportMessage="Test running";}
        public void StopTest(){model.Stop(Now());ExportSummary();}
        public void RequestExit(){if(quitting)return;quitting=true;cancel?.Cancel();}
        [Serializable] sealed class MetricsSummary {public string boundary="accepted input → Unity technical indicator; no scanout/photon",classification,deviceState;public long measurementStart,measurementEnd,qrVisible,technicalReady,joinDurationMs,phoneSequence,epoch,javaReceive,unityReceive,unityAccept,visualApply,endOfFrame;public int visualFrame,endFrame;public QaMetricsWire connections;public VisionSupervisor.QaCameraMetrics camera;public double unityUpdateFps;public QaPhoneMetricsWire phoneMetrics;public string crossDeviceOffset="NOT MEASURED";}
        QaMetricsWire CounterDelta(QaMetricsWire m)=>m==null?null:new QaMetricsWire{attempted=m.attempted-baseline.attempted,succeeded=m.succeeded-baseline.succeeded,reconnectAttempted=m.reconnectAttempted-baseline.reconnectAttempted,reconnectSucceeded=m.reconnectSucceeded-baseline.reconnectSucceeded};
        public void ExportSummary(){try{var summary=new MetricsSummary{classification=physicalConfirmed&&sample?.quality?.source=="physical"?"PHYSICAL (operator confirmed)":LiveQaModel.Classification(sample?.quality?.source),deviceState=joinClass,measurementStart=model.StartMs,measurementEnd=model.EndMs,qrVisible=qrVisible,technicalReady=joinEnd,joinDurationMs=joinEnd>0&&qrVisible>0?joinEnd-qrVisible:-1,phoneSequence=sample?.sequence??-1,epoch=sample?.connectionEpoch??-1,javaReceive=sample?.serverReceiveTimestamp??-1,unityReceive=sample?.unityReceiveTimestamp??-1,unityAccept=sample?.unityAcceptTimestamp??-1,visualApply=applied,endOfFrame=presented,visualFrame=appliedFrame,endFrame=presentedFrame,connections=CounterDelta(java.QaMetrics),camera=vision.QaMetrics,phoneMetrics=PhoneMetrics,unityUpdateFps=fps};string directory=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"GorillaEscape-QA-Summaries");Directory.CreateDirectory(directory);File.WriteAllText(Path.Combine(directory,"qa-"+Now()+".json"),JsonUtility.ToJson(summary,true));exportMessage="Summary saved: ~/GorillaEscape-QA-Summaries (metrics only)";}catch(IOException){exportMessage="Summary export failed";}}
        [Serializable]sealed class SurfaceSummary{public bool visualApplied,cameraCleanup,javaCleanup,controlsRestored,qaPhoneObserved;public long phoneAccepted,previewOverwritten;public int visualFrame,endOfFrame;public double unityUpdateFps;public string previewState;}
        [Serializable]sealed class CameraCleanup{public bool complete,forced,controlsRestored;public int pid,exitCode;public long accepted;public string source;}
        bool WantsToQuit(){if(allowQuit)return true;RequestExit();return false;}
        async Task Close(){if(java==null)return;ClearQr();preview?.Dispose();if(previewClosing!=null)await previewClosing;java.RequestStop();if(javaRun!=null)await javaRun;if(visionRun!=null)await visionRun;await vision.StopAsync();if(preview?.Run!=null)await preview.Run;fusion.Clear();sample=null;ClearQr();if(cameraTexture!=null){Destroy(cameraTexture);cameraTexture=null;}Debug.Log("QA_SURFACE_SUMMARY "+JsonUtility.ToJson(new SurfaceSummary{visualApplied=applied>0&&presented>0,qaPhoneObserved=qaPhoneObserved,phoneAccepted=java.PhoneInputs.Accepted,visualFrame=appliedFrame,endOfFrame=presentedFrame,unityUpdateFps=fps,previewState=preview?.State,previewOverwritten=preview?.Overwritten??0,cameraCleanup=vision.CleanupComplete,javaCleanup=java.CleanupComplete,controlsRestored=vision.ControlsRestored}));if(vision.Summary!=null)Debug.Log("CAMERA_LAB_METRICS "+vision.Summary);Debug.Log("CAMERA_LAB_CLEANUP "+JsonUtility.ToJson(new CameraCleanup{complete=vision.CleanupComplete,forced=vision.Forced,controlsRestored=vision.ControlsRestored,pid=vision.Pid,exitCode=vision.ExitCode??-1,accepted=cameras.Accepted,source=cameras.Latest?.source}));cameras.Clear();java.Dispose();vision.Dispose();}
        async void OnDestroy(){Application.wantsToQuit-=WantsToQuit;await ShutdownAsync();cancel?.Dispose();if(java!=null)Application.runInBackground=priorBackground;}
    }
}
