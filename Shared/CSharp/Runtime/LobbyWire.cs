using System;
namespace GorillaEscape.Contracts
{
    [Serializable] public sealed class LobbyPlayerWire {
        public int playerId; public string deviceSessionId,connectionState,displayName;
        public long connectionEpoch; public bool networkReady,inputReady,playerReady;
    }
    [Serializable] public sealed class LobbySnapshotWire {
        public int lobbyVersion; public string sessionId,phase; public LobbyPlayerWire[] players; public string roomCode;
    }
    [Serializable] public sealed class LobbyCommandWire {
        public string kind,phase="",sessionId,deviceSessionId="";
        public int playerId; public long connectionEpoch; public bool ready;
    }
    [Serializable] public sealed class LobbyCommandResult { public bool accepted; public string code; }
}
