using System;
namespace GorillaEscape.Contracts
{
    [Serializable] public sealed class PhoneValues {
        public string availability;
        public bool hasX,hasY,hasZ,hasAlpha,hasBeta,hasGamma,hasAngle,hasPressed;
        public float x,y,z,alpha,beta,gamma,angle; public bool pressed;
    }
    [Serializable] public sealed class PhoneQuality { public string source,status; }
    [Serializable] public sealed class PhoneInputWire {
        public int playerId;public long sequence,connectionEpoch,clientTimestamp,serverReceiveTimestamp;
        public string sessionId,deviceSessionId,connectionState,messageType;public string[] capabilities;
        public PhoneQuality quality;
        public PhoneValues acceleration,accelerationIncludingGravity,rotationRate,orientation,screenOrientation,touch;
    }
    [Serializable] public sealed class PhonePongPayload { public PhoneInputWire[] phoneInputs; public bool hasLobbyResult; public LobbySnapshotWire lobby; public LobbyCommandResult lobbyResult; }
    [Serializable] public sealed class PhonePong {
        public int ipcVersion,sequence;public string type,instanceId,connectionId;public PhonePongPayload payload;
    }
}
