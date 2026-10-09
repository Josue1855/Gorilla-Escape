using System;
using System.Collections.Generic;
using GorillaEscape.Contracts;

namespace GorillaEscape.Input
{
    public enum OfficialActionState { CANDIDATE, CONFIRMED, CANCELLED }
    public enum ActionDecision { CREATED, CONFIRMED, IDEMPOTENT, CANCELLED, INVALID, STALE, DUPLICATE, CAPACITY, UNKNOWN, TERMINAL, CONFLICT }

    // Non-secret identity. Session/device prevent collisions when a player slot is reused.
    public readonly struct ActionKey : IEquatable<ActionKey>
    {
        public readonly string SessionId, DeviceSessionId, ActionKind;
        public readonly int PlayerId;
        public readonly long ConnectionEpoch, SourceSequence;
        public ActionKey(string session, string device, int player, long epoch, long sequence, string kind)
        { SessionId=session; DeviceSessionId=device; PlayerId=player; ConnectionEpoch=epoch; SourceSequence=sequence; ActionKind=kind; }
        public bool Equals(ActionKey other) => SessionId==other.SessionId && DeviceSessionId==other.DeviceSessionId && PlayerId==other.PlayerId && ConnectionEpoch==other.ConnectionEpoch && SourceSequence==other.SourceSequence && ActionKind==other.ActionKind;
        public override bool Equals(object other) => other is ActionKey key && Equals(key);
        public override int GetHashCode() => HashCode.Combine(SessionId,DeviceSessionId,PlayerId,ConnectionEpoch,SourceSequence,ActionKind);
    }
    public sealed class OfficialActionResult
    {
        public ActionKey ActionId { get; }
        public string OutcomeCode { get; }
        public long ConfirmedAt { get; }
        internal OfficialActionResult(ActionKey key,string code,long at) { ActionId=key; OutcomeCode=code; ConfirmedAt=at; }
    }
    public sealed class OfficialActionRecord
    {
        public ActionKey ActionId { get; }
        public OfficialActionState State { get; }
        public OfficialActionResult Result { get; }
        internal OfficialActionRecord(ActionKey key,OfficialActionState state,OfficialActionResult result=null) { ActionId=key; State=state; Result=result; }
    }

    // Unity authority API: accepts observations, never creates actions from TOUCH/ACK/READY.
    // All transitions share one lock; returned records/results are immutable snapshots.
    public sealed class OfficialActionLedger
    {
        public const int MaximumActions=256;
        public const long FreshnessMilliseconds=2000;
        private sealed class Player
        {
            public string Session,Device; public long Epoch,Sequence=-1,Received,Consumed=-1;
            public bool Usable,LobbyKnown,LobbyReady;
        }
        private readonly object gate=new object();
        private readonly Player[] players=new Player[4];
        private readonly Dictionary<ActionKey,OfficialActionRecord> actions=new Dictionary<ActionKey,OfficialActionRecord>();
        private readonly Func<long> now;
        private string session;
        private bool ended;
        public OfficialActionLedger(Func<long> clock=null) { now=clock??(()=>DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()); }
        public int Count { get { lock(gate) return actions.Count; } }
        private static bool Code(string text) { if(string.IsNullOrEmpty(text)||text.Length>64)return false; foreach(char c in text)if(!(c>='a'&&c<='z'||c>='A'&&c<='Z'||c>='0'&&c<='9'||c=='_'||c=='-'||c=='.'))return false; return true; }
        private bool Fresh(Player p) { long age=now()-p.Received; return !ended&&p.Usable&&(!p.LobbyKnown||p.LobbyReady)&&age>=0&&age<FreshnessMilliseconds; }
        private void CancelPlayer(int id) { var keys=new List<ActionKey>(); foreach(var pair in actions)if(pair.Key.PlayerId==id&&pair.Value.State==OfficialActionState.CANDIDATE)keys.Add(pair.Key); foreach(var key in keys)actions[key]=new OfficialActionRecord(key,OfficialActionState.CANCELLED); }
        private Player Identity(string sid,string device,int id,long epoch,bool replaceDevice=false)
        {
            if(ended||id<1||id>4||epoch<1||string.IsNullOrEmpty(sid)||sid.Length>128||string.IsNullOrEmpty(device)||device.Length>128)return null;
            if(session==null)session=sid; if(session!=sid)return null;
            var p=players[id-1];
            if(p!=null && (p.Device!=device&&!replaceDevice || p.Device==device&&epoch<p.Epoch))return null;
            if(p==null||p.Device!=device||epoch>p.Epoch) { CancelPlayer(id); p=new Player{Session=sid,Device=device,Epoch=epoch}; players[id-1]=p; }
            return p;
        }
        internal void ObserveInput(PhoneInputWire input)
        {
            lock(gate) {
                var p=Identity(input.sessionId,input.deviceSessionId,input.playerId,input.connectionEpoch); if(p==null)return;
                if(input.messageType=="SERVER_STATE") { if(input.connectionState!="ACTIVE") {p.Usable=false;CancelPlayer(input.playerId);} return; }
                if(input.sequence<=p.Sequence)return;
                p.Sequence=input.sequence;p.Received=input.serverReceiveTimestamp;
                p.Usable=input.connectionState=="ACTIVE"&&input.quality!=null&&input.quality.status!="unavailable"&&(input.messageType=="MOTION_SAMPLE"||input.messageType=="TOUCH");
                if(!Fresh(p))CancelPlayer(input.playerId);
            }
        }
        internal void ObserveLobby(LobbySnapshotWire snapshot)
        {
            lock(gate) {
                if(snapshot==null||snapshot.players==null)return;
                var seen=new bool[4];
                foreach(var wire in snapshot.players) {
                    var p=Identity(snapshot.sessionId,wire.deviceSessionId,wire.playerId,wire.connectionEpoch,true); if(p==null)continue;
                    seen[wire.playerId-1]=true;p.LobbyKnown=true;p.LobbyReady=wire.networkReady&&wire.inputReady&&wire.connectionState!="DISCONNECTED";
                    if(!p.LobbyReady)CancelPlayer(wire.playerId);
                }
                for(int i=0;i<4;i++)if(!seen[i]&&players[i]!=null) {players[i].Usable=false;CancelPlayer(i+1);}
            }
        }
        public ActionDecision TryBegin(ActionKey key,out OfficialActionRecord record)
        {
            lock(gate) {
                record=null;
                if(!Code(key.ActionKind)||key.PlayerId<1||key.PlayerId>4||key.SourceSequence<0)return ActionDecision.INVALID;
                if(actions.TryGetValue(key,out record))return ActionDecision.DUPLICATE;
                var p=players[key.PlayerId-1];
                if(p==null||key.SessionId!=p.Session||key.DeviceSessionId!=p.Device||key.ConnectionEpoch!=p.Epoch||key.SourceSequence!=p.Sequence||!Fresh(p))return ActionDecision.STALE;
                // One candidate per consumed source sample, even across semantic aliases.
                if(key.SourceSequence<=p.Consumed)return ActionDecision.DUPLICATE;
                if(actions.Count>=MaximumActions)return ActionDecision.CAPACITY;
                p.Consumed=key.SourceSequence;record=new OfficialActionRecord(key,OfficialActionState.CANDIDATE);actions.Add(key,record);return ActionDecision.CREATED;
            }
        }
        public ActionDecision Confirm(ActionKey key,string outcomeCode,out OfficialActionRecord record)
        {
            lock(gate) {
                if(!actions.TryGetValue(key,out record))return ActionDecision.UNKNOWN;
                if(!Code(outcomeCode))return ActionDecision.INVALID;
                if(record.State==OfficialActionState.CONFIRMED)return record.Result.OutcomeCode==outcomeCode?ActionDecision.IDEMPOTENT:ActionDecision.CONFLICT;
                if(record.State==OfficialActionState.CANCELLED)return ActionDecision.TERMINAL;
                var p=players[key.PlayerId-1];
                if(p==null||p.Epoch!=key.ConnectionEpoch||!Fresh(p)) {record=new OfficialActionRecord(key,OfficialActionState.CANCELLED);actions[key]=record;return ActionDecision.TERMINAL;}
                record=new OfficialActionRecord(key,OfficialActionState.CONFIRMED,new OfficialActionResult(key,outcomeCode,now()));actions[key]=record;return ActionDecision.CONFIRMED;
            }
        }
        public ActionDecision Cancel(ActionKey key)
        {
            lock(gate) {if(!actions.TryGetValue(key,out var record))return ActionDecision.UNKNOWN;if(record.State!=OfficialActionState.CANDIDATE)return ActionDecision.TERMINAL;actions[key]=new OfficialActionRecord(key,OfficialActionState.CANCELLED);return ActionDecision.CANCELLED;}
        }
        public bool TryGet(ActionKey key,out OfficialActionRecord record) {lock(gate)return actions.TryGetValue(key,out record);}
        // Transport loss cancels candidates, keeps confirmed results queryable until session reset.
        internal void EndSession() {lock(gate){ended=true;for(int id=1;id<=4;id++)CancelPlayer(id);}}
        internal void Reset() {lock(gate){actions.Clear();Array.Clear(players,0,players.Length);session=null;ended=false;}}
    }
}
