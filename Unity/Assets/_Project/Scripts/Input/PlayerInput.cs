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
        public GorillaEscape.Contracts.CameraInputWire Frame;
        public GorillaEscape.Contracts.CameraSubject Subject;
        public bool PlayerDetected;
        public float TrackingConfidence;
        public long Timestamp;
    }
    public struct PhoneInput
    {
        public int PlayerId;
        // Raw protocol sample retains per-axis availability, units and clock provenance.
        public GorillaEscape.Contracts.PhoneInputWire ProtocolSample;
        public bool HasAcceleration, HasAngularVelocity, HasOrientation;
        public Vector3 Acceleration, AngularVelocity;
        public Quaternion Orientation;
        public bool ActionPressed, IsConnected;
        public long Timestamp, Sequence;
    }
}
