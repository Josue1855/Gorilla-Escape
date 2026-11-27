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
        public long unityReceiveTimestamp,unityAcceptTimestamp;
        public int playerId;public long sequence,connectionEpoch,clientTimestamp,serverReceiveTimestamp;
        public string sessionId,deviceSessionId,connectionState,messageType;public string[] capabilities;
        public PhoneQuality quality;
        public PhoneValues acceleration,accelerationIncludingGravity,rotationRate,orientation,screenOrientation,touch;
    }
    [Serializable] public sealed class QaPhoneMetricsWire {
        public int qaVersion,playerId;public long connectionEpoch,receivedAt;
        public bool hasMotionHz,hasOrientationHz,hasEmissionHz,hasRttP50,hasRttP95;
        public float motionHz,orientationHz,emissionHz,rttP50,rttP95;
    }
    [Serializable] public sealed class QaConnectionWire {public bool recoveryRequested;public string firewallState;public string admissionMode,requestedMode,resolvedMode,bootstrapState,tunnelState,publicOrigin,lanOrigin,fallbackReason,wanAvailability,webRtcPath;public long tunnelStartupMs,fallbackMs;}
    [Serializable] public sealed class QaHostingWire {public QaAdmissionWire prepare;public string origin,fingerprint,prepareUrl;}
    [Serializable] public sealed class QaAdmissionWire {public string state,modules;public long ttlMs;public int size;}
    [Serializable] public sealed class QaMetricsWire {public long attempted,succeeded,reconnectAttempted,reconnectSucceeded;}
    [Serializable] public sealed class PhonePongPayload { public QaPhoneMetricsWire[] qaPhones; public QaConnectionWire qaConnection; public QaHostingWire qaHosting; public QaAdmissionWire qa; public QaMetricsWire qaMetrics; public PhoneInputWire[] phoneInputs; public bool hasLobbyResult; public LobbySnapshotWire lobby; public LobbyCommandResult lobbyResult; }
    [Serializable] public sealed class PhonePong {
        public int ipcVersion,sequence;public string type,instanceId,connectionId;public PhonePongPayload payload;
    }
}
