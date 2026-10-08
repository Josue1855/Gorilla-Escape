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
    public sealed class InputFusion {
        public const int PhoneHistoryCapacity=32;
        readonly FusionSettings settings; readonly PhoneInputStore phones;
        readonly Dictionary<int,List<PhoneInputWire>> history=new Dictionary<int,List<PhoneInputWire>>();
        public CameraInputStore Cameras {get;}=new CameraInputStore();
        public PlayerLock Association {get;}
        public long DuplicateOrReordered {get;private set;} public long Candidates {get;private set;}
        public InputFusion(PhoneInputStore phones,FusionSettings settings){settings.Validate();this.phones=phones;this.settings=settings;Association=new PlayerLock(settings);}
        // Observes the already validated PhoneInputStore, never replaces mobile/IPC validation.
        public void ObservePhone(PhoneInputWire s){
            if(s==null||s.playerId<1||s.playerId>4||s.messageType!="MOTION_SAMPLE")return;
            if(!history.TryGetValue(s.playerId,out var list)){list=new List<PhoneInputWire>();history.Add(s.playerId,list);}
            if(list.Count>0){var p=list[list.Count-1];if(p.sessionId!=s.sessionId||p.deviceSessionId!=s.deviceSessionId||p.connectionEpoch!=s.connectionEpoch)list.Clear();else if(s.sequence<=p.sequence){DuplicateOrReordered++;return;}}
            list.Add(s);if(list.Count>PhoneHistoryCapacity)list.RemoveAt(0);
        }
        public bool ObserveCamera(CameraInputWire frame,long received){if(!Cameras.Accept(frame,received))return false;Association.Observe(frame);return true;}
        static bool Fresh(long timestamp,long now,long age,long future)=>timestamp>=now-age&&timestamp<=now+future;
        public InputFusionFrame Evaluate(int player,long now){
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
                    if(!Fresh(c.captureTimestamp,now,settings.maxCameraAge,settings.futureTolerance))continue;
                    foreach(var s in c.subjects){
                        if(s.subjectId!=Association.SubjectId||s.ambiguous||s.confidence<settings.minCameraConfidence)continue;
                        long delta=Math.Abs(p.serverReceiveTimestamp-c.captureTimestamp);
                        if(delta<best||delta==best&&(p.serverReceiveTimestamp>(result.Phone?.serverReceiveTimestamp??-1)||p.serverReceiveTimestamp==(result.Phone?.serverReceiveTimestamp??-1)&&p.sequence>(result.Phone?.sequence??-1)||c.frameSequence>(camera?.frameSequence??0))){best=delta;camera=c;subject=s;result.Phone=p;}
                    }
                }
            }
            if(camera!=null)phone=result.Phone;
            if(active&&Cameras.Latest!=null&&!Cameras.Fresh(now,settings.maxCameraAge,settings.futureTolerance))Association.MarkLost();
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
