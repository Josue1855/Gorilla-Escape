using System;
namespace GorillaEscape.Contracts
{
    // Display-only, sent by Unity over authenticated IPC, then by the reliable RTC heartbeat ACK.
    [Serializable] public sealed class GamePresentationWire
    {
        public int version=2,playerId;
        public string gameId="lobby",phase="Ready";
        public GameDisplayStats stats=new GameDisplayStats();
        public bool Valid()
        {
            bool game=gameId=="lobby"||gameId=="bowling"||gameId=="boxing"||gameId=="baseball"||gameId=="drums"||gameId=="coconut-slice"||gameId=="whack"||gameId=="log-balance";
            bool state=phase=="Ready"||phase=="Countdown"||phase=="Playing"||phase=="Paused"||phase=="Won"||phase=="Lost"||phase=="Finished";
            return version==2&&game&&state&&playerId>=0&&playerId<=4&&(gameId=="lobby"?playerId==0:playerId>0)&&stats!=null&&stats.Valid();
        }
        public GamePresentationWire Copy()=>new GamePresentationWire{version=version,playerId=playerId,gameId=gameId,phase=phase,stats=stats.Copy()};
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
