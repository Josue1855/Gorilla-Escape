using System;
using GorillaEscape.Minigames.LogBalance;
namespace GorillaEscape.Gameplay.Input
{
    // Neutral calibration consumes only distinct, current fused observations. No raw input-store route.
    public sealed class LogBalanceFusedControl
    {
        public bool Calibrated {get;private set;}
        public float Value {get;private set;}
        public string Status {get;private set;}="Mantén el teléfono cómodo y vertical para preparar el control.";
        float neutral,min,max,sum;
        int count;
        long start,lastTime,lastSequence=-1;
        public void Reset()
        {Calibrated=false;Value=neutral=min=max=sum=0;count=0;start=lastTime=0;lastSequence=-1;Status="Preparando el control… Mantén el teléfono quieto.";}
        void Candidate(JungleFusedFrame frame)
        {start=lastTime=frame.PhoneTimestamp;lastSequence=frame.PhoneSequence;min=max=sum=frame.Motion.gamma;count=1;}
        public bool Observe(JungleFusedFrame frame,LogBalanceRules rules,bool idle)
        {
            if(frame==null)return false;
            if(frame.Baseline)Reset();
            if(!frame.Motion.hasScreen||Math.Abs(frame.Motion.screen)>1||Math.Abs(frame.Motion.gamma)>90)
            {Reset();Status="Sostén el teléfono vertical, con la parte superior hacia arriba.";return false;}
            if(!Calibrated)
            {
                if(!idle)return false;
                if(count==0){Candidate(frame);return false;}
                if(frame.PhoneSequence==lastSequence)return false;
                float gamma=frame.Motion.gamma;
                if(frame.PhoneSequence<lastSequence||frame.PhoneTimestamp<=lastTime
                    ||frame.PhoneTimestamp-lastTime>rules.freshnessMilliseconds||Math.Max(max,gamma)-Math.Min(min,gamma)>2)
                {Candidate(frame);return false;}
                min=Math.Min(min,gamma);max=Math.Max(max,gamma);sum+=gamma;count++;
                lastTime=frame.PhoneTimestamp;lastSequence=frame.PhoneSequence;
                if(count<8||lastTime-start<450)return false;
                neutral=sum/count;Calibrated=true;
            }
            Value=Math.Max(-1,Math.Min(1,(frame.Motion.gamma-neutral)/rules.tiltDegrees));
            Status="Control listo";return true;
        }
    }
}
