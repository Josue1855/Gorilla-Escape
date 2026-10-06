using System;

namespace GorillaEscape.Contracts
{
    public static class ProtocolVersion { public const int Current = 1; }

    [Serializable]
    public class ProtocolEnvelope<T>
    {
        public int version;
        public string type;
        public string sessionId;
        public int playerId;
        public long sequence;
        public long timestamp;
        public T payload;
    }

    [Serializable]
    public struct WireVector3 { public float x, y, z; }
    [Serializable]
    public struct WireQuaternion { public float x, y, z, w; }
    [Serializable]
    public struct SensorPayload
    {
        public WireVector3 acceleration;
        public WireVector3 gyro;
        public WireQuaternion orientation;
    }
    [Serializable]
    public sealed class SensorEnvelope : ProtocolEnvelope<SensorPayload> {}
}
