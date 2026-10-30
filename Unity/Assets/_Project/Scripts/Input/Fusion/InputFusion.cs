using System;
using System.Collections.Generic;
using GorillaEscape.Contracts;
using GorillaEscape.CameraTracking;
namespace GorillaEscape.Input.Fusion
{
    public enum AlignmentStatus {ALIGNED,UNALIGNED,PHONE_DEGRADED,CAMERA_DEGRADED,NO_CANDIDATE,ASSOCIATION_LOST,INACTIVE}
    public sealed class InputFusionFrame {
        public int PlayerId,SubjectId; public bool PhoneFresh,CameraFresh,Aligned,Eligible;
        public long PhoneAge=long.MaxValue,CameraAge=long.MaxValue,AlignmentDelta=long.MaxValue,EvaluationTimestamp;
        public AlignmentStatus Status;public PlayerLockState LockState;
        public PhoneInputWire Phone;public CameraInputWire Camera;public CameraSubject Subject;
        // Diagnostic raw inputs remain observable when freshness/association rejects eligibility.
        public PhoneInputWire RawPhone; public CameraInputWire RawCamera;
        public long RawPhoneAge=long.MaxValue,RawCameraAge=long.MaxValue;
        public bool PhoneReceiptFresh,CameraFrameFresh;
        public string[] AvailableSignals; public float? CameraConfidence;
        public CameraInput AssociatedCamera;
        // No gesture, action, power, score, fabricated orientation or position inference.
    }
    // Presentation estimate only. Never pass this to action recognition or connection gating.
    public sealed class PhoneVisualEstimate {
        public long SourceTimestamp,EstimatedTimestamp;
        public PhoneValues Acceleration,AccelerationIncludingGravity,RotationRate,Orientation;
    }
    public sealed class InputFusion {
        public const int PhoneHistoryCapacity=32;
        readonly FusionSettings settings; readonly PhoneInputStore phones;
        readonly Dictionary<int,List<PhoneInputWire>> history=new Dictionary<int,List<PhoneInputWire>>();
        bool partialHands;
        public CameraInputStore Cameras {get;}=new CameraInputStore();
        public PlayerLock Association {get;}
        public int RetainedPhoneSamples(int player) => history.TryGetValue(player,out var samples)?samples.Count:0;
        public long DuplicateOrReordered {get;private set;} public long Candidates {get;private set;}
        public InputFusion(PhoneInputStore phones,FusionSettings settings){settings.Validate();this.phones=phones;this.settings=settings;Association=new PlayerLock(settings);}
        // Observes the already validated PhoneInputStore, never replaces mobile/IPC validation.
        public void ObservePhone(PhoneInputWire s){
            if(s==null||s.playerId<1||s.playerId>4||s.messageType!="MOTION_SAMPLE")return;
            if(!history.TryGetValue(s.playerId,out var list)){list=new List<PhoneInputWire>();history.Add(s.playerId,list);}
            if(list.Count>0){var p=list[list.Count-1];if(p.sessionId!=s.sessionId||p.deviceSessionId!=s.deviceSessionId||p.connectionEpoch!=s.connectionEpoch)list.Clear();else if(s.sequence<=p.sequence){DuplicateOrReordered++;return;}}
            list.Add(s);if(list.Count>PhoneHistoryCapacity)list.RemoveAt(0);
        }
        public bool ObserveCamera(CameraInputWire frame,long received,bool allowPartialSlice=false){if(!Cameras.Accept(frame,received))return false;partialHands=allowPartialSlice;Association.Observe(frame,allowPartialSlice);return true;}
        static bool Number(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
        static float Estimate(float a,float b,long dt,long ahead,float maxChange,bool angular){
            if(!Number(a)||!Number(b)||dt<8||dt>250)return b;
            float delta=b-a;
            if(angular){delta%=360;if(delta>180)delta-=360;if(delta< -180)delta+=360;}
            float change=delta*ahead/dt;
            return b+Math.Max(-maxChange,Math.Min(maxChange,change));
        }
        static PhoneValues VisualValues(PhoneValues old,PhoneValues current,long dt,long ahead,float limit,bool angular){
            if(current==null)return null;
            var result=new PhoneValues{availability=current.availability,hasX=current.hasX,hasY=current.hasY,hasZ=current.hasZ,
                hasAlpha=current.hasAlpha,hasBeta=current.hasBeta,hasGamma=current.hasGamma,
                x=current.x,y=current.y,z=current.z,alpha=current.alpha,beta=current.beta,gamma=current.gamma};
            if(old==null)return result;
            if(current.hasX&&old.hasX)result.x=Estimate(old.x,current.x,dt,ahead,limit,angular);
            if(current.hasY&&old.hasY)result.y=Estimate(old.y,current.y,dt,ahead,limit,angular);
            if(current.hasZ&&old.hasZ)result.z=Estimate(old.z,current.z,dt,ahead,limit,angular);
            if(current.hasAlpha&&old.hasAlpha)result.alpha=Estimate(old.alpha,current.alpha,dt,ahead,limit,angular);
            if(current.hasBeta&&old.hasBeta)result.beta=Estimate(old.beta,current.beta,dt,ahead,limit,angular);
            if(current.hasGamma&&old.hasGamma)result.gamma=Estimate(old.gamma,current.gamma,dt,ahead,limit,angular);
            return result;
        }
        public bool TryPhoneVisualEstimate(int player,long now,out PhoneVisualEstimate estimate){
            estimate=null;
            if(!phones.TryLatest(player,out var current)||current==null||current.messageType!="MOTION_SAMPLE"||current.connectionState!="ACTIVE"
                ||!history.TryGetValue(player,out var list)||list.Count==0)return false;
            var latest=list[list.Count-1];
            long age=now-latest.serverReceiveTimestamp;
            if(age<0||age>120||current.sequence!=latest.sequence||current.connectionEpoch!=latest.connectionEpoch
                ||current.sessionId!=latest.sessionId||current.deviceSessionId!=latest.deviceSessionId)return false;
            var prior=list.Count>1?list[list.Count-2]:null;
            long dt=prior==null?0:latest.serverReceiveTimestamp-prior.serverReceiveTimestamp;
            long ahead=Math.Min(age,80);
            estimate=new PhoneVisualEstimate{SourceTimestamp=latest.serverReceiveTimestamp,EstimatedTimestamp=now,
                Acceleration=VisualValues(prior?.acceleration,latest.acceleration,dt,ahead,4f,false),
                AccelerationIncludingGravity=VisualValues(prior?.accelerationIncludingGravity,latest.accelerationIncludingGravity,dt,ahead,4f,false),
                RotationRate=VisualValues(prior?.rotationRate,latest.rotationRate,dt,ahead,100f,false),
                Orientation=VisualValues(prior?.orientation,latest.orientation,dt,ahead,12f,true)};
            return true;
        }
        static bool Fresh(long timestamp,long now,long age,long future)=>timestamp>=now-age&&timestamp<=now+future;
        public InputFusionFrame Evaluate(int player,long now) => Evaluate(player,now,false);
        // Keep the current camera frame authoritative, but account for inference latency by
        // matching it to observed phone history. Never recover an older camera pose.
        public InputFusionFrame EvaluateLatest(int player,long now) => Evaluate(player,now,true);
        InputFusionFrame Evaluate(int player,long now,bool latestOnly){
            var result=new InputFusionFrame{PlayerId=player,SubjectId=Association.SubjectId,LockState=Association.State,EvaluationTimestamp=now,Status=AlignmentStatus.NO_CANDIDATE,AvailableSignals=Array.Empty<string>()};
            if(player<1||player>4)return result;
            phones.TryLatest(player,out var current);
            result.RawPhone=current;result.RawCamera=Cameras.Latest;
            result.RawPhoneAge=current==null?long.MaxValue:Math.Max(0,now-current.serverReceiveTimestamp);
            result.RawCameraAge=Cameras.Age(now);
            result.PhoneReceiptFresh=current!=null&&Fresh(current.serverReceiveTimestamp,now,settings.maxPhoneAge,settings.futureTolerance);
            result.CameraFrameFresh=Cameras.Fresh(now,settings.maxCameraAge,settings.futureTolerance);
            bool connected=current!=null&&current.connectionState!="DISCONNECTED";
            history.TryGetValue(player,out var samples);
            PhoneInputWire phone=null;CameraInputWire camera=null;CameraSubject subject=null;long best=long.MaxValue;
            // Camera is turn-active only. Select nearest compatible pair, not a forced 1:1 cadence.
            bool active=Association.PlayerId==player;
            if(connected&&samples!=null)foreach(var p in samples){
                if(p.sessionId!=current.sessionId||p.deviceSessionId!=current.deviceSessionId||p.connectionEpoch!=current.connectionEpoch||!Fresh(p.serverReceiveTimestamp,now,settings.maxPhoneAge,settings.futureTolerance))continue;
                if(phone==null||p.serverReceiveTimestamp>phone.serverReceiveTimestamp)phone=p;
                if(!active||Association.State!=PlayerLockState.LOCKED)continue;
                foreach(var c in Cameras.Frames){
                    if(latestOnly && c.frameSequence!=Cameras.Latest.frameSequence)continue;
                    if(!Fresh(c.captureTimestamp,now,settings.maxCameraAge,settings.futureTolerance))continue;
                    foreach(var s in c.subjects){
                        if(s.subjectId!=Association.SubjectId||s.ambiguous||(partialHands?!CameraInputStore.SliceSubjectUsable(s):s.confidence<settings.minCameraConfidence))continue;
                        long delta=Math.Abs(p.serverReceiveTimestamp-c.captureTimestamp);
                        if(delta<best||delta==best&&(p.serverReceiveTimestamp>(result.Phone?.serverReceiveTimestamp??-1)||p.serverReceiveTimestamp==(result.Phone?.serverReceiveTimestamp??-1)&&p.sequence>(result.Phone?.sequence??-1)||c.frameSequence>(camera?.frameSequence??0))){best=delta;camera=c;subject=s;result.Phone=p;}
                    }
                }
            }
            if(camera!=null)phone=result.Phone;
            if(active&&Cameras.Latest!=null&&!Cameras.Fresh(now,settings.maxCameraAge+FusionSettings.AssociationGraceMilliseconds,settings.futureTolerance))Association.MarkLost();
            if(active&&camera==null&&Association.State==PlayerLockState.LOCKED){var last=Cameras.Latest;if(last!=null&&Fresh(last.captureTimestamp,now,settings.maxCameraAge,settings.futureTolerance))foreach(var s in last.subjects)if(s.subjectId==Association.SubjectId){camera=last;subject=s;break;}}
            result.Phone=phone;result.Camera=camera;result.Subject=subject;result.LockState=Association.State;
            result.PhoneFresh=phone!=null;result.CameraFresh=camera!=null&&active&&Association.State==PlayerLockState.LOCKED;
            result.PhoneAge=phone==null?long.MaxValue:Math.Max(0,now-phone.serverReceiveTimestamp);
            result.CameraAge=camera==null?long.MaxValue:Math.Max(0,now-camera.captureTimestamp);
            if(phone!=null&&camera!=null)result.AlignmentDelta=Math.Abs(phone.serverReceiveTimestamp-camera.captureTimestamp);
            result.Aligned=result.PhoneFresh&&result.CameraFresh&&result.AlignmentDelta<=settings.alignmentWindow;
            result.Eligible=result.Aligned;
            result.Status=!active?AlignmentStatus.INACTIVE:!result.PhoneFresh&&!result.CameraFresh?AlignmentStatus.NO_CANDIDATE:Association.State!=PlayerLockState.LOCKED?AlignmentStatus.ASSOCIATION_LOST:!result.PhoneFresh?AlignmentStatus.PHONE_DEGRADED:!result.CameraFresh?AlignmentStatus.CAMERA_DEGRADED:result.Aligned?AlignmentStatus.ALIGNED:AlignmentStatus.UNALIGNED;
            result.CameraConfidence=subject==null?(float?)null:subject.confidence;
            if(result.CameraFresh)result.AssociatedCamera=new CameraInput{PlayerId=player,Frame=camera,Subject=subject,PlayerDetected=true,TrackingConfidence=subject.confidence,Timestamp=camera.captureTimestamp};
            var signals=new List<string>();if(phone!=null){foreach(var pair in new[]{new KeyValuePair<string,PhoneValues>("acceleration",phone.acceleration),new KeyValuePair<string,PhoneValues>("rotationRate",phone.rotationRate),new KeyValuePair<string,PhoneValues>("orientation",phone.orientation),new KeyValuePair<string,PhoneValues>("touch",phone.touch)})if(pair.Value!=null&&pair.Value.availability!="unavailable")signals.Add(pair.Key);}
            if(result.CameraFresh)foreach(var l in subject.landmarks)if(l.visible)signals.Add(l.name);
            result.AvailableSignals=signals.ToArray();if(result.Eligible)Candidates++;return result;
        }
        public void Clear(){history.Clear();Cameras.Clear();Association.Release();}
    }
}
