using System;
namespace GorillaEscape.Contracts
{
    // Display-only, sent by Unity over authenticated IPC, then by the reliable RTC heartbeat ACK.
    [Serializable] public sealed class GamePresentationWire
    {
        public int version=3,playerId;
        public string gameId="lobby",phase="Ready";
        public string screen="menu",contextId=Guid.NewGuid().ToString("N"),hand="right";
        public bool phoneRequired;
        public GameDisplayStats stats=new GameDisplayStats();
        public bool Valid()
        {
            bool game=KnownGame(gameId)||gameId=="lobby";
            bool state=phase=="Ready"||phase=="Countdown"||phase=="Playing"||phase=="Paused"||phase=="Won"||phase=="Lost"||phase=="Finished";
            return version==3&&game&&state&&playerId>=0&&playerId<=4&&(gameId=="lobby"?playerId==0:playerId>0)&&stats!=null&&stats.Valid()
                &&(screen=="menu"||screen=="connection"||screen=="game")&&(gameId=="lobby"?screen!="game":screen=="game")
                &&contextId!=null&&contextId.Length==32&&Guid.TryParseExact(contextId,"N",out _)&&(hand=="right"||hand=="left");
        }
        public static bool KnownGame(string id)=>id=="bowling"||id=="boxing"||id=="baseball"||id=="drums"||id=="coconut-slice"||id=="jungle-keeper"||id=="whack"||id=="log-balance";
        public GamePresentationWire Copy()=>new GamePresentationWire{version=version,playerId=playerId,gameId=gameId,phase=phase,stats=stats.Copy(),screen=screen,contextId=contextId,hand=hand,phoneRequired=phoneRequired};
    }
    [Serializable] public sealed class MobileUiCommandWire
    {
        public string action,target,contextId,sessionId,deviceSessionId,requestId;
        public int playerId;
        public long connectionEpoch,receivedAt;
        public bool Valid(GamePresentationWire context,LobbySnapshotWire lobby,long now)
        {
            if(context==null||!context.Valid()||lobby?.players==null||contextId!=context.contextId||playerId!=1
                ||sessionId!=lobby.sessionId||now<receivedAt||now-receivedAt>2500)return false;
            var player=Array.Find(lobby.players,p=>p.playerId==playerId);
            if(player==null||!player.networkReady||player.connectionState=="DISCONNECTED"||player.deviceSessionId!=deviceSessionId||player.connectionEpoch!=connectionEpoch)return false;
            return action=="select-game"&&context.gameId=="lobby"&&GamePresentationWire.KnownGame(target)
                ||action=="menu"&&target==""||action=="restart"&&context.screen=="game"&&target==""
                ||action=="start"&&context.screen=="game"&&context.phase=="Ready"&&target==""
                ||action=="set-hand"&&(context.gameId=="lobby"||context.gameId=="bowling")&&(target=="left"||target=="right");
        }
    }
    [Serializable] public sealed class GameDisplayStats
    {
        public bool hasScore,hasProgress,hasElapsed,hasBalance,hasDistance;
        public double score;
        public float progress,elapsed,balance,distance;
        static bool Range(double value,double min,double max)=>!double.IsNaN(value)&&!double.IsInfinity(value)&&value>=min&&value<=max;
        public bool Valid()=>Range(score,0,1000000000)&&Range(progress,0,1)&&Range(elapsed,0,3600)&&Range(balance,-1,1)&&Range(distance,0,10000);
        public GameDisplayStats Copy()=>(GameDisplayStats)MemberwiseClone();
    }
}
