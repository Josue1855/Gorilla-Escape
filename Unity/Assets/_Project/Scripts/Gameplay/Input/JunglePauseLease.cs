namespace GorillaEscape.Gameplay.Input
{
    // Presentation only: observed input remains the sole source of gameplay actions.
    public sealed class HandVisualPrediction
    {
        const float Horizon = .05f, MaxDistance = .025f, Expiry = .25f;
        float x,y,vx,vy,observedAt;
        bool valid;
        public void Observe(float px,float py,float velocityX,float velocityY,float now)
        {
            if(float.IsNaN(px)||float.IsInfinity(px)||float.IsNaN(py)||float.IsInfinity(py)||float.IsNaN(now)||float.IsInfinity(now)
                ||float.IsNaN(velocityX)||float.IsInfinity(velocityX)||float.IsNaN(velocityY)||float.IsInfinity(velocityY)){Reset();return;}
            float elapsed=now-observedAt;
            if(!valid||elapsed<=0||elapsed>Expiry){x=px;y=py;vx=vy=0;}
            else
            {
                float dx=px-x,dy=py-y;
                float distance=(float)System.Math.Sqrt(dx*dx+dy*dy);
                // Tiny camera fluctuations are damped; larger deliberate movements catch up quickly.
                float response=System.Math.Min(1f,System.Math.Max(0f,(distance-.008f)/.08f));
                float rate=4f+response*18f;
                float alpha=1f-(float)System.Math.Exp(-rate*elapsed);
                x+=dx*alpha;y+=dy*alpha;
                float motion=System.Math.Min(1f,System.Math.Max(0f,(distance-.012f)/.05f));
                vx=velocityX*motion;vy=velocityY*motion;
            }
            observedAt=now;valid=true;
        }
        public void Reset() { valid=false; }
        public bool TryPosition(float now,out float px,out float py)
        {
            px=x;py=y;
            float age=now-observedAt;
            if(!valid||age<0||age>Expiry)return false;
            float dt=System.Math.Min(age,Horizon),dx=vx*dt,dy=vy*dt;
            float distance=(float)System.Math.Sqrt(dx*dx+dy*dy);
            if(distance>MaxDistance){dx*=MaxDistance/distance;dy*=MaxDistance/distance;}
            px=System.Math.Max(0,System.Math.Min(1,x+dx));
            py=System.Math.Max(0,System.Math.Min(1,y+dy));
            return true;
        }
    }
    // Keep inputs gated immediately, but do not resume until tracking has stayed stable.
    public sealed class TrackingPauseStability
    {
        public const float ResumeSeconds = .5f;
        public const float NoticeDelaySeconds = .3f;
        bool paused = true;
        float readySince = -1, lostSince = -1;
        public bool Paused => paused;
        public bool ShowNotice(float now) => paused && lostSince >= 0 && now - lostSince >= NoticeDelaySeconds;
        public void Reset() { paused = true; readySince = lostSince = -1; }
        public bool Update(bool ready, float now,float lossGraceSeconds=0)
        {
            if (!ready)
            {
                if (lostSince < 0) lostSince = now;
                // Brief camera delivery gaps do not restart an otherwise stable recovery.
                // The input/action boundary still rejects every unavailable frame immediately.
                if (now-lostSince<lossGraceSeconds)return paused;
                readySince = -1;
                paused = true;
            }
            else if (paused)
            {
                lostSince = -1;
                if (readySince < 0) readySince = now;
                if (now - readySince >= ResumeSeconds) { paused = false; lostSince = -1; }
            }
            else lostSince=-1;
            return paused;
        }
    }
    // A technical pause owns only the scale it changed. Never resumes a pre-existing pause.
    public sealed class JunglePauseLease
    {
        public bool Owned {get;private set;}
        float prior;
        public float Apply(bool paused,float current)
        {
            if(paused)
            {
                if(!Owned&&current>0){prior=current;Owned=true;}
                return Owned?0:current;
            }
            if(!Owned)return current;
            Owned=false;
            return current==0?prior:current;
        }
    }
}
