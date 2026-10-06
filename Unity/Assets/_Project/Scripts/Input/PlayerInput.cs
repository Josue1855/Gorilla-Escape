using UnityEngine;

namespace GorillaEscape.Input
{
    // Specification section 9; game contracts only, no transport/hardware dependencies.
    public struct PlayerMotion
    {
        public float Power, Direction, Elevation, Spin, AngularVelocity, Stability, Timing, GestureConfidence;
    }
    public struct PlayerInput
    {
        public int PlayerId;
        public PlayerMotion Motion;
        public Vector2 LeftHand, RightHand;
        public bool Throw, Smash, Swing, PlayerTracked;
    }
    public struct CameraInput
    {
        public int PlayerId;
        public bool PlayerDetected;
        public Vector2 BodyPosition, LeftHand, RightHand, LeftArmDirection, RightArmDirection;
        public bool ThrowGesture, SmashGesture;
        public float TrackingConfidence;
        public long Timestamp;
    }
    public struct PhoneInput
    {
        public int PlayerId;
        public Vector3 Acceleration, AngularVelocity;
        public Quaternion Orientation;
        public bool ActionPressed, IsConnected;
        public long Timestamp, Sequence;
    }
}
