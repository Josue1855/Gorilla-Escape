namespace GorillaEscape.Input
{
    public struct CameraInput
    {
        public int PlayerId;
        public GorillaEscape.Contracts.CameraInputWire Frame;
        public GorillaEscape.Contracts.CameraSubject Subject;
        public bool PlayerDetected;
        public float TrackingConfidence;
        public long Timestamp;
    }
}
