using System;
using GorillaEscape.Contracts;
using GorillaEscape.CameraTracking;

namespace GorillaEscape.Input
{
    // Engine-independent typed projections; never a second network protocol or JSON bridge.
    public sealed class JungleMotionPacket
    {
        public string type {get;internal set;} = "motion";
        public long time {get;internal set;}
        public float alpha {get;internal set;}
        public float beta {get;internal set;}
        public float gamma {get;internal set;}
        public float screen {get;internal set;}
        public bool hasScreen {get;internal set;}
        public float ax {get;internal set;}
        public float ay {get;internal set;}
        public float az {get;internal set;}
        public float rx {get;internal set;}
        public float ry {get;internal set;}
        public float rz {get;internal set;}
        public int confirm {get;internal set;}
        public int recenter {get;internal set;}
    }
    public sealed class JunglePosePacket
    {
        public bool detected {get;internal set;}
        public bool leftDetected {get;internal set;}
        public bool rightDetected {get;internal set;}
        public string hand {get;internal set;} = "right";
        public float wristX {get;internal set;}
        public float wristY {get;internal set;}
        public float bodyX {get;internal set;}
        public float confidence {get;internal set;}
        public float leftWristX {get;internal set;}
        public float leftWristY {get;internal set;}
        public float rightWristX {get;internal set;}
        public float rightWristY {get;internal set;}
        public long time {get;internal set;}
    }

    public sealed class JungleInputAdapter
    {
        string session, device;
        long epoch = -1, sequence = -1, cameraSequence = -1;
        bool pressed;
        int confirmations, recenters;
        public bool TryMotion(PhoneInputWire input, long now, out JungleMotionPacket packet)
        {
            packet = null;
            if (input == null || input.playerId != 1 || input.messageType != "MOTION_SAMPLE"
                || input.connectionState == "DISCONNECTED" || string.IsNullOrEmpty(input.sessionId)
                || string.IsNullOrEmpty(input.deviceSessionId) || input.connectionEpoch < 1
                || input.sequence < 0 || !Fresh(input.serverReceiveTimestamp, now)) return false;
            bool changed = session != input.sessionId || device != input.deviceSessionId || epoch != input.connectionEpoch;
            if (!changed && input.sequence <= sequence) return false;
            // Games need gravity-inclusive m/s², rotation in degrees/s and real Euler angles.
            // Partial/unavailable values are rejected instead of silently becoming zero.
            var a = input.accelerationIncludingGravity;
            var r = input.rotationRate;
            var o = input.orientation;
            if (!Vector(a) || !Angles(r) || !Angles(o)) return false;
            if (changed) { session = input.sessionId; device = input.deviceSessionId; epoch = input.connectionEpoch; pressed = false; }
            sequence = input.sequence;
            bool nextPressed = input.touch != null && input.touch.hasPressed && input.touch.pressed;
            if (nextPressed && !pressed)
            {
                // Two virtual touch zones defined by the controller UI; the wire remains generic TOUCH data.
                if (input.touch.hasX && Finite(input.touch.x) && input.touch.x >= .5f) recenters++;
                else confirmations++;
            }
            pressed = nextPressed;
            packet = new JungleMotionPacket {
                time = input.serverReceiveTimestamp, ax = a.x, ay = a.y, az = a.z,
                rx = r.alpha, ry = r.beta, rz = r.gamma, alpha = o.alpha, beta = o.beta, gamma = o.gamma,
                hasScreen = input.screenOrientation!=null&&input.screenOrientation.hasAngle&&Finite(input.screenOrientation.angle),
                screen = input.screenOrientation != null && input.screenOrientation.hasAngle && Finite(input.screenOrientation.angle)
                    ? input.screenOrientation.angle : 0,
                confirm = confirmations, recenter = recenters
            };
            return true;
        }
        public bool TryPose(CameraInputWire frame, int subjectId, long now, out JunglePosePacket packet)
        {
            packet = null;
            if (!CameraInputStore.Valid(frame) || !Fresh(frame.captureTimestamp, now)
                || frame.frameSequence <= cameraSequence || frame.trackingState != "TRACKING") return false;
            var subject = Array.Find(frame.subjects, s => s.subjectId == subjectId);
            if (subject == null || subject.ambiguous || subject.confidence < .6f) return false;
            var left = Array.Find(subject.landmarks, l => l.name == "left-wrist");
            var right = Array.Find(subject.landmarks, l => l.name == "right-wrist");
            bool leftVisible = Visible(left), rightVisible = Visible(right);
            if (!leftVisible && !rightVisible) return false;
            cameraSequence = frame.frameSequence;
            // Phase 0 is unmirrored. The alpha expects a selfie mirror; reflect X exactly once.
            packet = new JunglePosePacket {
                time = frame.captureTimestamp, detected = rightVisible,
                leftDetected = leftVisible, rightDetected = rightVisible,
                leftWristX = leftVisible ? 1 - left.x : 0, leftWristY = leftVisible ? left.y : 0,
                rightWristX = rightVisible ? 1 - right.x : 0, rightWristY = rightVisible ? right.y : 0,
                wristX = rightVisible ? 1 - right.x : 0, wristY = rightVisible ? right.y : 0,
                bodyX = 1 - subject.centerX, confidence = subject.confidence
            };
            return true;
        }
        static bool Fresh(long timestamp, long now) => timestamp >= 0 && timestamp >= now - 250 && timestamp <= now + 20;
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static bool Vector(PhoneValues value) => value != null && value.availability == "present"
            && value.hasX && value.hasY && value.hasZ && Finite(value.x) && Finite(value.y) && Finite(value.z);
        static bool Angles(PhoneValues value) => value != null && value.availability == "present"
            && value.hasAlpha && value.hasBeta && value.hasGamma && Finite(value.alpha) && Finite(value.beta) && Finite(value.gamma);
        static bool Visible(CameraLandmark value) => value != null && value.visible && value.visibility >= .6f
            && value.presence >= .6f && value.x >= 0 && value.x <= 1 && value.y >= 0 && value.y <= 1;
    }
}
