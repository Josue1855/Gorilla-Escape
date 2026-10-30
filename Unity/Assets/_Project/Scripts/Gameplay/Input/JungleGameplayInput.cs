using System;
using GorillaEscape.Input;
using GorillaEscape.Input.Fusion;
using GorillaEscape.Contracts;
using GorillaEscape.CameraTracking;

namespace GorillaEscape.Gameplay.Input
{
    public enum JungleGame { None, Bowling, Boxing, Baseball, Drums, CoconutSlice, Whack, LogBalance }
    [Flags] public enum JungleAction
    {
        None=0, Throw=1, PunchLeft=2, PunchRight=4, Recover=8, Swing=16,
        DrumLeft=32, DrumRight=64, SliceLeft=128, SliceRight=256, Whack=512, Balance=1024, Save=2048
    }
    public enum JungleInputStatus { Menu, Waiting, TrackingLost, Unaligned, PartialInput, Reacquiring, Ready }
    public readonly struct JungleHand
    {
        public readonly float X,Y,VelocityX,VelocityY;
        public float Speed => (float)Math.Sqrt(VelocityX*VelocityX+VelocityY*VelocityY);
        public JungleHand(float x,float y,float vx,float vy) { X=x;Y=y;VelocityX=vx;VelocityY=vy; }
    }
    public sealed class JungleFusedFrame
    {
        public long Revision {get;internal set;}
        public int Continuity {get;internal set;}
        public long PhoneSequence {get;internal set;}
        public long CameraSequence {get;internal set;}
        public long PhoneTimestamp {get;internal set;}
        public long CameraTimestamp {get;internal set;}
        public bool Baseline {get;internal set;}
        public bool NewMotion {get;internal set;}
        public bool NewPose {get;internal set;}
        public JungleMotionPacket Motion {get;internal set;}
        public JunglePosePacket Pose {get;internal set;}
        public JungleHand Left {get;internal set;}
        public JungleHand Right {get;internal set;}
        public float PhonePower {get;internal set;}
        public JungleAction Actions {get;internal set;}
    }

    // One bounded, typed gameplay boundary. Transport, Unity rendering and scores stay outside it.
    public sealed class JungleGameplayInput
    {
        // Conservative prototype outlier bound in normalized image widths/heights per second.
        public const float MaximumHandSpeed=12f;
        sealed class Pulse
        {
            bool armed;
            long next;
            public bool Observe(bool high,bool neutral,long now,int cooldown)
            {
                if(neutral)armed=true;
                if(!high||!armed)return false;
                armed=false; // A peak during cooldown is discarded, never delayed into a later action.
                if(now<next)return false;
                next=now+cooldown;return true;
            }
        }
        public JungleGame Game {get;private set;}
        public bool CameraOnly {get;private set;}
        public bool LeftHanded {get;set;}
        public JungleInputStatus Status {get;private set;}=JungleInputStatus.Menu;
        public bool Ready => Status==JungleInputStatus.Ready;
        public JungleFusedFrame Current {get;private set;}
        JungleInputAdapter adapter=new JungleInputAdapter();
        JungleMotionPacket motion;
        JunglePosePacket pose;
        JungleHand left,right;
        Pulse first=new Pulse(),second=new Pulse(),recovery=new Pulse();
        long phoneSequence=-1,cameraSequence=-1,poseTime,revision;
        string session,device;
        long epoch=-1;
        int subject,continuity;
        JungleAction consumed;
        bool broken=true;
        public static JungleGame ForScene(string scene) => scene=="BananaBowling"?JungleGame.Bowling
            :scene=="GorillaBoxing"?JungleGame.Boxing:scene=="JungleBaseball"?JungleGame.Baseball
            :scene=="JungleDrums"?JungleGame.Drums:scene=="CoconutSlice"?JungleGame.CoconutSlice
            :scene=="WhackAMole"?JungleGame.Whack:scene=="LogBalance"?JungleGame.LogBalance:JungleGame.None;
        public void Select(JungleGame game,bool cameraOnly=false)
        {
            CameraOnly=cameraOnly;
            Game=game;Break(game==JungleGame.None?JungleInputStatus.Menu:JungleInputStatus.Waiting);
            session=device=null;epoch=-1;subject=0;
        }
        void Break(JungleInputStatus status)
        {
            if(broken&&Status==status&&motion==null&&pose==null){Current=null;return;}
            if(!broken)continuity++;
            broken=true;Status=status;Current=null;consumed=JungleAction.None;
            phoneSequence=cameraSequence=-1;poseTime=0;motion=null;pose=null;
            adapter=new JungleInputAdapter();first=new Pulse();second=new Pulse();recovery=new Pulse();
            left=right=default;
        }
        static bool Fresh(long timestamp,long now) => timestamp>=0&&timestamp>=now-250&&timestamp<=now+20;
        static bool Wrist(CameraSubject subject,string name)
        {
            var p=Array.Find(subject.landmarks,l=>l.name==name);
            return p!=null&&p.visible&&p.visibility>=.6f&&p.presence>=.6f&&p.x>=0&&p.x<=1&&p.y>=0&&p.y<=1;
        }
        public static bool RequiresCamera(JungleGame game) => game!=JungleGame.None && game!=JungleGame.LogBalance;
        public static bool SupportsCameraOnly(JungleGame game) => game==JungleGame.CoconutSlice;
        public void UpdateCamera(CameraInputWire frame,PlayerLockState lockState,int subjectId,long now)
        {
            if(!CameraOnly||!SupportsCameraOnly(Game)||lockState!=PlayerLockState.LOCKED||!CameraInputStore.Valid(frame)
                ||!Fresh(frame.captureTimestamp,now)||frame.trackingState!="TRACKING")
            {Break(JungleInputStatus.TrackingLost);return;}
            var person=Array.Find(frame.subjects,s=>s.subjectId==subjectId);
            if(frame.subjects.Length!=1||!CameraInputStore.SliceSubjectUsable(person))
            {Break(JungleInputStatus.TrackingLost);return;}
            if(subject!=subjectId){Break(JungleInputStatus.Reacquiring);subject=subjectId;}
            bool baseline=broken;
            if(frame.frameSequence<cameraSequence){Break(JungleInputStatus.Reacquiring);return;}
            if(frame.frameSequence==cameraSequence)return;
            if(!adapter.TryPose(frame,subjectId,now,out var next,true)){Break(JungleInputStatus.PartialInput);return;}
            double dt=(frame.captureTimestamp-poseTime)/1000.0;
            bool derivative=!baseline&&pose!=null&&dt>=.008&&dt<=.25;
            bool leftDerivative=derivative&&next.leftDetected&&pose.leftDetected,rightDerivative=derivative&&next.rightDetected&&pose.rightDetected;
            left=new JungleHand(next.leftWristX,next.leftWristY,leftDerivative?(float)((next.leftWristX-pose.leftWristX)/dt):0,leftDerivative?(float)((next.leftWristY-pose.leftWristY)/dt):0);
            right=new JungleHand(next.rightWristX,next.rightWristY,rightDerivative?(float)((next.rightWristX-pose.rightWristX)/dt):0,rightDerivative?(float)((next.rightWristY-pose.rightWristY)/dt):0);
            if(leftDerivative&&left.Speed>MaximumHandSpeed){next.leftDetected=false;left=default;}
            if(rightDerivative&&right.Speed>MaximumHandSpeed){next.rightDetected=false;next.detected=false;right=default;}
            if(!next.leftDetected&&!next.rightDetected){Break(JungleInputStatus.Reacquiring);return;}
            pose=next;poseTime=frame.captureTimestamp;cameraSequence=frame.frameSequence;
            broken=false;Status=JungleInputStatus.Ready;
            JungleAction actions=baseline?JungleAction.None:Recognize(true,0,0,0,now);
            Current=new JungleFusedFrame{Revision=++revision,Continuity=continuity,CameraSequence=frame.frameSequence,
                CameraTimestamp=frame.captureTimestamp,Baseline=baseline,NewPose=!baseline,Pose=pose,Left=left,Right=right,Actions=actions};
            consumed=JungleAction.None;
        }
        // Log Balance uses only the phone orientation; no camera sample or player association is needed.
        public void UpdatePhone(PhoneInputWire phone,long now)
        {
            if(Game!=JungleGame.LogBalance){Break(JungleInputStatus.Waiting);return;}
            if(phone==null || phone.playerId!=1 || phone.messageType!="MOTION_SAMPLE" || phone.connectionState!="ACTIVE" || !Fresh(phone.serverReceiveTimestamp,now))
            {Break(JungleInputStatus.TrackingLost);return;}
            bool identity=session!=phone.sessionId || device!=phone.deviceSessionId || epoch!=phone.connectionEpoch;
            if(identity){Break(JungleInputStatus.Reacquiring);session=phone.sessionId;device=phone.deviceSessionId;epoch=phone.connectionEpoch;}
            if(!identity && phone.sequence<phoneSequence){Break(JungleInputStatus.Reacquiring);return;}
            if(!identity && phone.sequence==phoneSequence)return;
            if(!adapter.TryTilt(phone,now,out var next)){Break(JungleInputStatus.PartialInput);return;}
            bool baseline=broken;broken=false;Status=JungleInputStatus.Ready;motion=next;phoneSequence=phone.sequence;
            Current=new JungleFusedFrame {Revision=++revision,Continuity=continuity,PhoneSequence=phone.sequence,
                PhoneTimestamp=phone.serverReceiveTimestamp,Baseline=baseline,NewMotion=!baseline,Motion=next,
                Actions=baseline?JungleAction.None:JungleAction.Balance};
            consumed=JungleAction.None;
        }
        public void Update(InputFusionFrame input,long now)
        {
            if(Game==JungleGame.None){Status=JungleInputStatus.Menu;Current=null;return;}
            if(input==null||!input.Eligible||input.LockState!=PlayerLockState.LOCKED)
            {Break(input?.Status==AlignmentStatus.UNALIGNED?JungleInputStatus.Unaligned:JungleInputStatus.TrackingLost);return;}
            var p=input.Phone;var c=input.Camera;var raw=input.RawPhone;var camera=input.RawCamera;
            if(p==null||c==null||raw==null||camera==null||!Fresh(raw.serverReceiveTimestamp,now)
                ||!Fresh(camera.captureTimestamp,now)||raw.connectionState!="ACTIVE"
                ||p.sequence>raw.sequence||p.sessionId!=raw.sessionId||p.deviceSessionId!=raw.deviceSessionId
                ||p.connectionEpoch!=raw.connectionEpoch||c.frameSequence!=camera.frameSequence
                ||!Fresh(p.serverReceiveTimestamp,now)||!Fresh(c.captureTimestamp,now)
                ||Math.Abs(p.serverReceiveTimestamp-c.captureTimestamp)>100)
            {Break(JungleInputStatus.Unaligned);return;}
            // A buffered match must not conceal missing sensors in the live phone stream.
            if(!JungleInputAdapter.MotionAvailable(raw,now))
            {Break(JungleInputStatus.PartialInput);return;}
            if(!CameraInputStore.Valid(camera)||camera.trackingState!="TRACKING") {Break(JungleInputStatus.TrackingLost);return;}
            var person=Array.Find(camera.subjects,s=>s.subjectId==input.SubjectId);
            bool both=Game==JungleGame.Boxing||Game==JungleGame.Drums||Game==JungleGame.CoconutSlice||Game==JungleGame.LogBalance;
            if(person==null||person.ambiguous||(Game==JungleGame.Bowling?!CameraInputStore.SliceSubjectUsable(person):person.confidence<.6f)
                ||!Wrist(person,Game==JungleGame.Bowling&&LeftHanded?"left-wrist":"right-wrist")||(both&&!Wrist(person,"left-wrist")))
            {Break(JungleInputStatus.TrackingLost);return;}
            bool identity=session!=p.sessionId||device!=p.deviceSessionId||epoch!=p.connectionEpoch||subject!=input.SubjectId;
            if(identity) {Break(JungleInputStatus.Reacquiring);session=p.sessionId;device=p.deviceSessionId;epoch=p.connectionEpoch;subject=input.SubjectId;}
            bool baseline=broken;
            bool newMotion=p.sequence!=phoneSequence,newPose=c.frameSequence!=cameraSequence;
            if(p.sequence<phoneSequence||c.frameSequence<cameraSequence) {Break(JungleInputStatus.Reacquiring);return;}
            if(newMotion&&(!adapter.TryMotion(p,now,out motion)||!motion.hasScreen)) {Break(JungleInputStatus.PartialInput);return;}
            if(newPose)
            {
                if(!adapter.TryPose(c,input.SubjectId,now,out var next,Game==JungleGame.Bowling)) {Break(JungleInputStatus.PartialInput);return;}
                double dt=(c.captureTimestamp-poseTime)/1000.0;
                bool derivative=!baseline&&pose!=null&&dt>=.008&&dt<=.25;
                left=new JungleHand(next.leftWristX,next.leftWristY,derivative?(float)((next.leftWristX-pose.leftWristX)/dt):0,derivative?(float)((next.leftWristY-pose.leftWristY)/dt):0);
                right=new JungleHand(next.rightWristX,next.rightWristY,derivative?(float)((next.rightWristX-pose.rightWristX)/dt):0,derivative?(float)((next.rightWristY-pose.rightWristY)/dt):0);
                if(derivative&&((Game==JungleGame.Bowling&&LeftHanded?left.Speed:right.Speed)>MaximumHandSpeed||(both&&left.Speed>MaximumHandSpeed))){Break(JungleInputStatus.Reacquiring);return;}
                pose=next;poseTime=c.captureTimestamp;
            }
            broken=false;Status=JungleInputStatus.Ready;
            if(!newMotion&&!newPose)return;
            phoneSequence=p.sequence;cameraSequence=c.frameSequence;
            float angular=(float)Math.Sqrt(motion.rx*motion.rx+motion.ry*motion.ry+motion.rz*motion.rz);
            float gravity=(float)Math.Sqrt(motion.ax*motion.ax+motion.ay*motion.ay+motion.az*motion.az);
            float power=angular+gravity*7;
            if(float.IsInfinity(power)||float.IsNaN(power)){Break(JungleInputStatus.PartialInput);return;}
            var actions=baseline?JungleAction.None:Recognize(newPose,power,angular,gravity,now);
            Current=new JungleFusedFrame {Revision=++revision,Continuity=continuity,PhoneSequence=p.sequence,
                CameraSequence=c.frameSequence,PhoneTimestamp=p.serverReceiveTimestamp,CameraTimestamp=c.captureTimestamp,
                Baseline=baseline,NewMotion=!baseline&&newMotion,NewPose=!baseline&&newPose,
                Motion=motion,Pose=pose,Left=left,Right=right,PhonePower=power,Actions=actions};
            consumed=JungleAction.None;
        }
        JungleAction Recognize(bool poseChanged,float power,float angular,float gravity,long now)
        {
            JungleAction result=JungleAction.None;
            var throwingHand=LeftHanded?left:right;
            if(Game==JungleGame.Bowling&&first.Observe(power>330&&throwingHand.Speed>.35f,power<180&&throwingHand.Speed<.25f,now,600))result|=JungleAction.Throw;
            if(Game==JungleGame.Boxing)
            {
                if(poseChanged&&first.Observe(left.Speed>.72f,left.Speed<.25f,now,340))result|=JungleAction.PunchLeft;
                if(second.Observe(power>215&&right.Speed>.72f,power<150&&right.Speed<.25f,now,340))result|=JungleAction.PunchRight;
                if(recovery.Observe(power>245&&right.Speed>.35f,power<150&&right.Speed<.25f,now,300))result|=JungleAction.Recover;
            }
            if(Game==JungleGame.Baseball&&first.Observe(power>260&&right.Speed>.55f,power<155&&right.Speed<.25f,now,200))result|=JungleAction.Swing;
            if(Game==JungleGame.Drums)
            {
                if(poseChanged&&first.Observe(left.VelocityY>.62f,left.VelocityY<.20f,now,200))result|=JungleAction.DrumLeft;
                if(second.Observe(right.VelocityY>.62f&&power>225,right.VelocityY<.20f&&power<150,now,200))result|=JungleAction.DrumRight;
            }
            if(Game==JungleGame.CoconutSlice&&poseChanged)
            {
                if(left.Speed>.72f)result|=JungleAction.SliceLeft;
                if(right.Speed>.72f)result|=JungleAction.SliceRight;
            }
            if(Game==JungleGame.CoconutSlice&&pose!=null&&(pose.leftDetected||pose.rightDetected))
                result|=JungleAction.Save;
            if(Game==JungleGame.LogBalance)result|=JungleAction.Balance;
            float whack=Math.Max(0,gravity-9.81f)*18+angular;
            if(Game==JungleGame.Whack&&first.Observe(right.VelocityY>.72f&&whack>235,right.VelocityY<.2f&&whack<150,now,220))result|=JungleAction.Whack;
            return result;
        }
        public bool Authorize(JungleAction action,long sourceRevision,long now)
        {
            int bit=(int)action;
            if(!Ready||Current==null||Current.Baseline||sourceRevision!=Current.Revision||bit==0||(bit&(bit-1))!=0
                ||(!CameraOnly&&!Fresh(Current.PhoneTimestamp,now))||(RequiresCamera(Game)&&!Fresh(Current.CameraTimestamp,now))
                ||(Current.Actions&action)==0||(consumed&action)!=0)return false;
            consumed|=action;return true;
        }
    }
}
