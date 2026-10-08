using System;
using GorillaEscape.Contracts;
namespace GorillaEscape.Input.Fusion
{
    public enum PlayerLockState {UNASSIGNED,ACQUIRING,LOCKED,LOST,RECOVERING}
    [Serializable] public sealed class FusionSettings {
        // Laboratory settings, neither sporting thresholds nor hardware accuracy claims.
        public long maxPhoneAge=250,maxCameraAge=250,alignmentWindow=100,futureTolerance=20;
        public float minCameraConfidence=.6f,roiMinX=.2f,roiMaxX=.8f,maxSpatialStep=.2f;
        public int acquisitionFrames=2,recoveryFrames=2;
        public void Validate(){if(float.IsNaN(minCameraConfidence)||float.IsNaN(roiMinX)||float.IsNaN(roiMaxX)||float.IsNaN(maxSpatialStep)||maxPhoneAge<0||maxCameraAge<0||alignmentWindow<0||futureTolerance<0||acquisitionFrames<1||recoveryFrames<1||minCameraConfidence<0||minCameraConfidence>1||roiMinX<0||roiMaxX>1||roiMinX>=roiMaxX||maxSpatialStep<=0||maxSpatialStep>1)throw new ArgumentException("Invalid lab fusion configuration");}
    }
    public sealed class PlayerLock {
        readonly FusionSettings settings; int evidence;float lastX,lastY;long sequence;
        public PlayerLockState State {get;private set;}=PlayerLockState.UNASSIGNED;
        public int PlayerId {get;private set;} public int SubjectId {get;private set;}
        public PlayerLock(FusionSettings settings){settings.Validate();this.settings=settings;}
        public void Ready(int player){if(player<1||player>4)throw new ArgumentOutOfRangeException(nameof(player));PlayerId=player;SubjectId=0;evidence=0;sequence=0;State=PlayerLockState.ACQUIRING;}
        public void Release(){PlayerId=SubjectId=0;evidence=0;sequence=0;State=PlayerLockState.UNASSIGNED;}
        public void MarkLost(){if(State!=PlayerLockState.UNASSIGNED){State=PlayerLockState.LOST;evidence=0;}}
        public void Observe(CameraInputWire frame){
            if(State==PlayerLockState.UNASSIGNED||frame.frameSequence<=sequence)return;sequence=frame.frameSequence;
            CameraSubject chosen=null;int candidates=0;
            foreach(var s in frame.subjects)if(s.centerX>=settings.roiMinX&&s.centerX<=settings.roiMaxX&&s.confidence>=settings.minCameraConfidence){candidates++;if(SubjectId==0||s.subjectId==SubjectId)chosen=s;}
            // Conservative: crossing/second subject inside ROI invalidates lock instead of guessing identity.
            if(chosen==null||candidates!=1||chosen.ambiguous||SubjectId!=0&&(Math.Abs(chosen.centerX-lastX)>settings.maxSpatialStep||Math.Abs(chosen.centerY-lastY)>settings.maxSpatialStep)){MarkLost();return;}
            if(SubjectId==0){SubjectId=chosen.subjectId;lastX=chosen.centerX;lastY=chosen.centerY;evidence=0;State=PlayerLockState.ACQUIRING;}
            if(State==PlayerLockState.LOST){State=PlayerLockState.RECOVERING;evidence=0;}
            lastX=chosen.centerX;lastY=chosen.centerY;
            if(State==PlayerLockState.ACQUIRING&&++evidence>=settings.acquisitionFrames||State==PlayerLockState.RECOVERING&&++evidence>=settings.recoveryFrames)State=PlayerLockState.LOCKED;
        }
    }
}
