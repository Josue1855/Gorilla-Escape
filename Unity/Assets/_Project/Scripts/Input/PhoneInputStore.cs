using System;
using System.Collections.Generic;
using GorillaEscape.Contracts;
namespace GorillaEscape.Input
{
    public sealed class PhoneInputStore {
        private readonly object gate=new object();
        private readonly Dictionary<int,PhoneInputWire> latest=new Dictionary<int,PhoneInputWire>();
        public OfficialActionLedger Actions { get; }
        public PhoneInputStore(Func<long> clock=null) { Actions=new OfficialActionLedger(clock); }
        public long Accepted {get;private set;} public long Rejected {get;private set;}
        public bool Accept(PhoneInputWire sample) {
            lock(gate) {
                if(sample==null||sample.playerId<1||sample.playerId>4||sample.sequence<0||sample.connectionEpoch<1||sample.quality==null||sample.serverReceiveTimestamp<0){Rejected++;return false;}
                if(latest.TryGetValue(sample.playerId,out var prior)&&prior.sessionId==sample.sessionId&&prior.deviceSessionId==sample.deviceSessionId) {
                    if(sample.messageType=="SERVER_STATE"&&sample.connectionEpoch==prior.connectionEpoch){prior.connectionState=sample.connectionState;Actions.ObserveInput(sample);return true;}
                    if(sample.connectionEpoch<prior.connectionEpoch||sample.connectionEpoch==prior.connectionEpoch&&sample.sequence<=prior.sequence){Rejected++;return false;}
                }
                latest[sample.playerId]=sample;Actions.ObserveInput(sample);Accepted++;return true;
            }
        }
        public bool TryLatest(int player,out PhoneInputWire sample){lock(gate)return latest.TryGetValue(player,out sample);}
        public bool TryPhoneInput(int player,out PhoneInput input){
            input=default;lock(gate){if(!latest.TryGetValue(player,out var s))return false;
                var a=s.acceleration;var r=s.rotationRate;
                input=new PhoneInput{PlayerId=player,ProtocolSample=s,Sequence=s.sequence,Timestamp=s.serverReceiveTimestamp,IsConnected=s.connectionState!="DISCONNECTED",HasAcceleration=a!=null&&a.hasX&&a.hasY&&a.hasZ,HasAngularVelocity=r!=null&&r.hasAlpha&&r.hasBeta&&r.hasGamma,HasOrientation=false,Orientation=UnityEngine.Quaternion.identity};
                if(input.HasAcceleration)input.Acceleration=new UnityEngine.Vector3(a.x,a.y,a.z);
                if(input.HasAngularVelocity)input.AngularVelocity=new UnityEngine.Vector3(r.alpha,r.beta,r.gamma);
                input.ActionPressed=s.touch!=null&&s.touch.hasPressed&&s.touch.pressed;return true;}
        }
        public long AgeMilliseconds(int player,long unityUnixMilliseconds){lock(gate)return latest.TryGetValue(player,out var s)?Math.Max(0,unityUnixMilliseconds-s.serverReceiveTimestamp):long.MaxValue;}
        public bool Fresh(int player,long now,long budgetMilliseconds){return budgetMilliseconds>=0&&AgeMilliseconds(player,now)<=budgetMilliseconds;}
        // Caller obtains the key from the current accepted Unity input, then explicitly begins/confirms.
        public bool TryActionSource(int player,string kind,out ActionKey key) {lock(gate){key=default;if(!latest.TryGetValue(player,out var s))return false;key=new ActionKey(s.sessionId,s.deviceSessionId,player,s.connectionEpoch,s.sequence,kind);return true;}}
        public void ObserveLobby(LobbySnapshotWire snapshot) {lock(gate)Actions.ObserveLobby(snapshot);}
        public void EndSession() {lock(gate)Actions.EndSession();}
        public void Clear(){lock(gate){latest.Clear();Actions.Reset();}}
    }
}
