using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GorillaEscape.Contracts;

namespace GorillaEscape.Network
{
    /// <summary>Unity's explicit lobby commands on its owned IPC; one command in flight, no unbounded queue.</summary>
    public sealed class LobbyAuthorityBridge
    {
        private readonly object gate=new object();
        private LobbySnapshotWire latest;
        private LobbyCommandWire pending;
        private TaskCompletionSource<LobbyCommandResult> completion;
        private bool sent;
        public LobbySnapshotWire Snapshot { get { lock(gate)return Copy(latest); } }
        public Task<LobbyCommandResult> SetPhaseAsync(string phase) {
            lock(gate) {
                if(phase!="STARTING"&&phase!="STARTED")throw new ArgumentException("LOBBY_TRANSITION");
                return Enqueue(new LobbyCommandWire{kind="SET_PHASE",phase=phase,sessionId=Session()});
            }
        }
        public Task<LobbyCommandResult> SetPlayerReadyAsync(int player,bool ready) {
            lock(gate) {
                Session();var identity=Array.Find(latest.players,p=>p.playerId==player);
                if(identity==null)throw new ArgumentException("PLAYER_ABSENT");
                return Enqueue(new LobbyCommandWire{kind="SET_PLAYER_READY",sessionId=latest.sessionId,
                    playerId=player,deviceSessionId=identity.deviceSessionId,connectionEpoch=identity.connectionEpoch,ready=ready});
            }
        }
        private string Session(){if(latest==null)throw new InvalidOperationException("LOBBY_NOT_CONNECTED");return latest.sessionId;}
        private Task<LobbyCommandResult> Enqueue(LobbyCommandWire command){
            if(completion!=null)throw new InvalidOperationException("LOBBY_COMMAND_PENDING");
            pending=command;sent=false;completion=new TaskCompletionSource<LobbyCommandResult>(TaskCreationOptions.RunContinuationsAsynchronously);return completion.Task;
        }
        public LobbyCommandWire TakeCommand(){lock(gate){if(pending==null||sent)return null;sent=true;return pending;}}
        public void Accept(LobbySnapshotWire snapshot,LobbyCommandResult result){
            Validate(snapshot);
            lock(gate) {
                if((sent&&completion!=null)!=(result!=null))throw new InvalidDataException("LOBBY_RECEIPT");
                if(result!=null&&(string.IsNullOrEmpty(result.code)||result.accepted!=(result.code=="ACCEPTED")))throw new InvalidDataException("LOBBY_RECEIPT");
                latest=Copy(snapshot);
                if(result!=null){var done=completion;completion=null;pending=null;sent=false;done.TrySetResult(result);}
            }
        }
        public void Reset(){lock(gate){latest=null;pending=null;sent=false;completion?.TrySetCanceled();completion=null;}}
        private static void Validate(LobbySnapshotWire s){
            if(s==null||s.lobbyVersion!=1||!Guid.TryParse(s.sessionId,out _)||s.players==null||s.players.Length>4
                ||(s.phase!="OPEN"&&s.phase!="STARTING"&&s.phase!="STARTED"))throw new InvalidDataException("LOBBY_SNAPSHOT");
            var players=new HashSet<int>();var devices=new HashSet<string>();
            foreach(var p in s.players)if(p==null||p.playerId<1||p.playerId>4||!players.Add(p.playerId)||!Guid.TryParse(p.deviceSessionId,out _)
                ||!devices.Add(p.deviceSessionId)||p.connectionEpoch<1||(p.connectionState!="CONNECTED"&&p.connectionState!="ACTIVE"&&p.connectionState!="DISCONNECTED")
                ||(p.inputReady&&!p.networkReady)||(p.playerReady&&!p.inputReady)||(p.connectionState=="DISCONNECTED"&&(p.networkReady||p.inputReady||p.playerReady)))throw new InvalidDataException("LOBBY_IDENTITY");
        }
        private static LobbySnapshotWire Copy(LobbySnapshotWire s){
            if(s==null)return null;
            var copy=new LobbySnapshotWire{lobbyVersion=s.lobbyVersion,sessionId=s.sessionId,phase=s.phase,players=new LobbyPlayerWire[s.players.Length]};
            for(int i=0;i<s.players.Length;i++){var p=s.players[i];copy.players[i]=new LobbyPlayerWire{playerId=p.playerId,deviceSessionId=p.deviceSessionId,connectionEpoch=p.connectionEpoch,
                connectionState=p.connectionState,networkReady=p.networkReady,inputReady=p.inputReady,playerReady=p.playerReady};}return copy;
        }
    }
}
