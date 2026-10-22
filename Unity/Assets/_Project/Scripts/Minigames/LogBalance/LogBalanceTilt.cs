using System;
using GorillaEscape.Contracts;
using GorillaEscape.Input;
namespace GorillaEscape.Minigames.LogBalance
{
    // Consumes the existing validated input store; no second transport, clock or player authority.
    public sealed class LogBalanceTilt
    {
        public bool Calibrated { get; private set; }
        public string Status { get; private set; } = "Preparando el control…";
        string session,device;
        long epoch;
        float neutral, minimum, maximum, sum;
        long candidateStart, lastTimestamp, lastSequence;
        int candidateCount;
        bool Matches(PhoneInputWire s)=>session==s.sessionId&&device==s.deviceSessionId&&epoch==s.connectionEpoch;
        void Identity(PhoneInputWire s){session=s.sessionId;device=s.deviceSessionId;epoch=s.connectionEpoch;}
        void Candidate(PhoneInputWire s){candidateStart=lastTimestamp=s.serverReceiveTimestamp;lastSequence=s.sequence;minimum=maximum=sum=s.orientation.gamma;candidateCount=1;}
        // Only call while idle/paused: an intentional lean during play must never become neutral.
        public bool PrepareAutomatically(PhoneInputStore store,int player,long now,LogBalanceRules rules)
        {
            if(!Read(store,player,now,rules.freshnessMilliseconds,out var s)){Reset();return false;}
            if(!Matches(s)){Reset();Identity(s);}
            if(Calibrated){
                if(Math.Abs(s.orientation.gamma-neutral)<=2f)return true;
                Calibrated=false;Candidate(s);Status="Preparando el control…";return false;
            }
            if(candidateCount==0){Candidate(s);Status="Preparando el control…";return false;}
            if(s.sequence==lastSequence)return false;
            float gamma=s.orientation.gamma;
            if(s.sequence<lastSequence || s.serverReceiveTimestamp<=lastTimestamp
                || s.serverReceiveTimestamp-lastTimestamp>rules.freshnessMilliseconds
                || Math.Max(maximum,gamma)-Math.Min(minimum,gamma)>2f){Candidate(s);return false;}
            minimum=Math.Min(minimum,gamma);maximum=Math.Max(maximum,gamma);sum+=gamma;candidateCount++;
            lastSequence=s.sequence;lastTimestamp=s.serverReceiveTimestamp;
            if(candidateCount<8||lastTimestamp-candidateStart<450)return false;
            neutral=sum/candidateCount;Calibrated=true;Status="Teléfono listo";return true;
        }
        public void Reset() { Calibrated=false;session=device=null;epoch=0;neutral=0;candidateCount=0;candidateStart=lastTimestamp=lastSequence=0;sum=minimum=maximum=0; }
        static bool Finite(float value) => !float.IsNaN(value)&&!float.IsInfinity(value);
        bool Read(PhoneInputStore store,int player,long now,int freshness,out PhoneInputWire sample)
        {
            sample=null;
            if(store==null||!store.TryLatest(player,out sample)||sample==null||sample.messageType!="MOTION_SAMPLE"
                ||(sample.connectionState!="CONNECTED"&&sample.connectionState!="ACTIVE")||sample.serverReceiveTimestamp>now+100
                ||!store.Fresh(player,now,freshness))
            {Status="Esperando al teléfono…";return false;}
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
            if(!Matches(s)){Reset();Status="Preparando el control…";return false;}
            if(!Calibrated){Status="Preparando el control…";return false;}
            tilt=Math.Max(-1,Math.Min(1,(s.orientation.gamma-neutral)/rules.tiltDegrees));Status="Teléfono listo";return true;
        }
    }
}
