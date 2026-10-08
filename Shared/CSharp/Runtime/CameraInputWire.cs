using System;
namespace GorillaEscape.Contracts
{
    // Camera coordinates are unmirrored normalized image coordinates, not Unity world metres.
    [Serializable] public sealed class CameraLandmark {
        public string name; public float x,y,z,visibility,presence; public bool visible;
    }
    [Serializable] public sealed class CameraSubject {
        public int subjectId; public float centerX,centerY,confidence; public bool ambiguous;
        public CameraLandmark[] landmarks;
    }
    [Serializable] public sealed class CameraInputWire {
        public int version,width,height; public long frameSequence,captureTimestamp,processTimestamp,unityReceiveTimestamp;
        public string source,quality,trackingState,clockDomain,captureTimeSource;
        public CameraSubject[] subjects;
    }
}
