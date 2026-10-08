using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GorillaEscape.Network;
using GorillaEscape.Input.Fusion;
using UnityEngine;
namespace GorillaEscape.CameraTracking
{
    // Opt-in lab wiring only: reuses exactly the existing managed Java/PhoneInputStore.
    public sealed class CameraFusionLabRunner : MonoBehaviour {
        VisionSupervisor vision; InputFusion fusion;CancellationTokenSource cancellation;Task start,stop;
        bool quitting,allowQuit;long revision;float nextLog;
        [Serializable] sealed class Command {public long revision;public int playerId;public bool ready;}
        [Serializable] sealed class Observation {
            public int playerId,subjectId; public long phoneSequence,phoneEpoch,cameraSequence,phoneTimestamp,cameraTimestamp,unityReceiveTimestamp,evaluatedAt,delta,phoneAge,cameraAge;
            public bool phoneFresh,cameraFresh,aligned,eligible;public string status,lockState,cameraSource,phoneSource;public string[]signals;public float cameraConfidence,marker;
            public Observation(InputFusionFrame f){playerId=f.PlayerId;subjectId=f.SubjectId;phoneSequence=f.Phone?.sequence??-1;phoneEpoch=f.Phone?.connectionEpoch??-1;cameraSequence=f.Camera?.frameSequence??-1;phoneTimestamp=f.Phone?.serverReceiveTimestamp??-1;cameraTimestamp=f.Camera?.captureTimestamp??-1;unityReceiveTimestamp=f.Camera?.unityReceiveTimestamp??-1;evaluatedAt=f.EvaluationTimestamp;delta=f.AlignmentDelta;phoneAge=f.PhoneAge;cameraAge=f.CameraAge;phoneFresh=f.PhoneFresh;cameraFresh=f.CameraFresh;aligned=f.Aligned;eligible=f.Eligible;status=f.Status.ToString();lockState=f.LockState.ToString();cameraSource=f.Camera?.source;phoneSource=f.Phone?.quality?.source;signals=f.AvailableSignals;cameraConfidence=f.CameraConfidence??-1;marker=f.Phone?.acceleration!=null&&f.Phone.acceleration.hasX?f.Phone.acceleration.x:-1;}
        }
        [Serializable]sealed class CameraObservation {
            public long sequence,captureTimestamp,processTimestamp,unityReceiveTimestamp;public int width,height,subjects,visibleLandmarks;public string source,trackingState;public float confidence=-1;
            public CameraObservation(GorillaEscape.Contracts.CameraInputWire f){sequence=f.frameSequence;captureTimestamp=f.captureTimestamp;processTimestamp=f.processTimestamp;unityReceiveTimestamp=f.unityReceiveTimestamp;width=f.width;height=f.height;subjects=f.subjects.Length;source=f.source;trackingState=f.trackingState;foreach(var s in f.subjects){confidence=Math.Max(confidence,s.confidence);foreach(var l in s.landmarks)if(l.visible)visibleLandmarks++;}}
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Launch(){if(Application.isEditor||Environment.GetEnvironmentVariable("GORILLA_CAMERA_LAB")!="1")return;var host=new GameObject("Camera fusion lab diagnostics");DontDestroyOnLoad(host);host.AddComponent<CameraFusionLabRunner>();}
        void Start(){vision=new VisionSupervisor();cancellation=new CancellationTokenSource();Application.wantsToQuit+=WantsToQuit;start=LaunchVision();}
        async Task LaunchVision(){try{await vision.StartAsync(Environment.GetEnvironmentVariable("GORILLA_CAMERA_PYTHON"),Environment.GetEnvironmentVariable("GORILLA_CAMERA_SCRIPT"),Environment.GetEnvironmentVariable("GORILLA_CAMERA_MODE")??"replay",Environment.GetEnvironmentVariable("GORILLA_CAMERA_FIXTURE"),Environment.GetEnvironmentVariable("GORILLA_CAMERA_MODEL"),Environment.GetEnvironmentVariable("GORILLA_CAMERA_DEVICE"),cancellation.Token);Debug.Log("CAMERA_LAB_READY");}catch(Exception){Debug.Log("CAMERA_LAB_ERROR "+vision.Error);}}
        void Update(){
            if(allowQuit)return;
            if(quitting){if(start!=null&&!start.IsCompleted)return;if(stop==null)stop=vision.StopAsync();if(!stop.IsCompleted)return;if(!vision.CleanupComplete)return;allowQuit=true;LogCleanup();Application.Quit();return;}
            if(fusion==null){var ipc=FindFirstObjectByType<IpcProbeRunner>();if(ipc?.PhoneInputs!=null)fusion=new InputFusion(ipc.PhoneInputs,Settings());}
            if(fusion==null)return;
            string path=Environment.GetEnvironmentVariable("GORILLA_FUSION_COMMAND");
            if(!string.IsNullOrEmpty(path)&&File.Exists(path))try{if(new FileInfo(path).Length<=1024){var c=JsonUtility.FromJson<Command>(File.ReadAllText(path));if(c!=null&&c.revision>revision&&c.playerId>=1&&c.playerId<=4){revision=c.revision;if(c.ready)fusion.Association.Ready(c.playerId);else fusion.Association.Release();Debug.Log("FUSION_COMMAND "+revision);}}}catch(IOException){}catch(ArgumentException){}
            // Four latest samples come from the previous, validated runtime. History is independently bounded.
            var owner=FindFirstObjectByType<IpcProbeRunner>();for(int player=1;player<=4;player++)if(owner.PhoneInputs.TryLatest(player,out var p))fusion.ObservePhone(p);
            if(vision.Take(out var frame,out var received)){if(fusion.ObserveCamera(frame,received))Debug.Log("CAMERA_INPUT_OBSERVED "+JsonUtility.ToJson(new CameraObservation(frame)));}
            vision.Poll();if(vision.State=="FAILED"){fusion.Association.MarkLost();if(stop==null){Debug.Log("CAMERA_LAB_ERROR "+vision.Error);stop=vision.StopAsync();}}
            if(Time.unscaledTime>=nextLog){nextLog=Time.unscaledTime+.05f;long now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();for(int player=1;player<=4;player++)Debug.Log("FUSION_OBSERVED "+JsonUtility.ToJson(new Observation(fusion.Evaluate(player,now))));}
        }
        static FusionSettings Settings(){string json=Environment.GetEnvironmentVariable("GORILLA_FUSION_SETTINGS");var s=string.IsNullOrEmpty(json)?new FusionSettings():JsonUtility.FromJson<FusionSettings>(json);s.Validate();return s;}
        bool WantsToQuit(){if(allowQuit)return true;quitting=true;cancellation.Cancel();return false;}
        void LogCleanup(){Debug.Log("CAMERA_LAB_CLEANUP "+JsonUtility.ToJson(new Cleanup{pid=vision.Pid,exitCode=vision.ExitCode??-1,complete=vision.CleanupComplete,forced=vision.Forced,state=vision.State,overwritten=vision.Overwritten}));if(vision.Summary!=null)Debug.Log("CAMERA_LAB_METRICS "+vision.Summary);}
        [Serializable]sealed class Cleanup{public int pid,exitCode;public bool complete,forced;public string state;public long overwritten;}
        async void OnDestroy(){Application.wantsToQuit-=WantsToQuit;cancellation?.Cancel();if(start!=null)await start;if(vision!=null){await vision.StopAsync();vision.Dispose();}fusion?.Clear();cancellation?.Dispose();}
    }
}
