using System;
using GorillaEscape.Contracts;
using GorillaEscape.Input;
namespace GorillaEscape.Minigames.LogBalance
{
    // Consumes the existing validated input store; no second transport, clock or player authority.
    public sealed class LogBalanceTilt
    {
        public bool Calibrated { get; private set; }
        public string Status { get; private set; } = "Calibra el teléfono en posición vertical.";
        string session,device;
        long epoch;
        float neutral;
        public void Reset() { Calibrated=false;session=device=null;epoch=0;neutral=0; }
        static bool Finite(float value) => !float.IsNaN(value)&&!float.IsInfinity(value);
        bool Read(PhoneInputStore store,int player,long now,int freshness,out PhoneInputWire sample)
        {
            sample=null;
            if(store==null||!store.TryLatest(player,out sample)||sample==null||sample.messageType!="MOTION_SAMPLE"
                ||(sample.connectionState!="CONNECTED"&&sample.connectionState!="ACTIVE")||sample.serverReceiveTimestamp>now+100
                ||!store.Fresh(player,now,freshness))
            {Status="Control sin datos recientes. Reconecta y calibra si cambió la conexión.";return false;}
            var o=sample.orientation;
            if(o==null||o.availability=="unavailable"||!o.hasGamma||!Finite(o.gamma)||Math.Abs(o.gamma)>90)
            {Status="Orientación no disponible. Activa los sensores del teléfono.";return false;}
            var screen=sample.screenOrientation;
            if(screen==null||!screen.hasAngle||!Finite(screen.angle)||Math.Abs(screen.angle)>1)
            {Status="Sostén el teléfono vertical, con la parte superior hacia arriba.";return false;}
            return true;
        }
        public bool Calibrate(PhoneInputStore store,int player,long now,LogBalanceRules rules)
        {
            Reset();
            if(!Read(store,player,now,rules.freshnessMilliseconds,out var s))return false;
            neutral=s.orientation.gamma;session=s.sessionId;device=s.deviceSessionId;epoch=s.connectionEpoch;
            Calibrated=true;Status="Posición neutra guardada.";return true;
        }
        public bool TryTilt(PhoneInputStore store,int player,long now,LogBalanceRules rules,out float tilt)
        {
            tilt=0;
            if(!Read(store,player,now,rules.freshnessMilliseconds,out var s))return false;
            if(!Calibrated||session!=s.sessionId||device!=s.deviceSessionId||epoch!=s.connectionEpoch)
            {Reset();Status="Calibra la posición neutra para esta conexión.";return false;}
            tilt=Math.Max(-1,Math.Min(1,(s.orientation.gamma-neutral)/rules.tiltDegrees));Status="Teléfono listo";return true;
        }
    }
}
