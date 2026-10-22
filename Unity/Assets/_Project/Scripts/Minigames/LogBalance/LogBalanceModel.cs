using System;

namespace GorillaEscape.Minigames.LogBalance
{
    [Serializable]
    public sealed class LogBalanceRules
    {
        public float countdownSeconds = 3, length = 18, forwardSpeed = .9f, maxSeconds = 40;
        public float tiltDegrees = 25, deadZone = .08f, smoothingSeconds = .18f, controlForce = .9f;
        public float disturbance = .09f, instability = .35f, difficulty = 1;
        public float balanceClamp = 1, fallThreshold = .85f, fallGraceSeconds = .65f;
        public int freshnessMilliseconds = 350;
        public bool coupledPhysics = true;
        public float lateralAcceleration = 3.6f, lateralDamping = 1.8f;
        public float weightTorque = 1.7f, rollDamping = 1.15f, rollRestoring = .85f;
        public float footingCoupling = 2.1f, stepTorque = .24f, windTorque = .16f;
        public float stepLength = .72f, progressDifficulty = .65f;
        public void Validate()
        {
            foreach (float v in new[] {countdownSeconds,length,forwardSpeed,maxSeconds,tiltDegrees,deadZone,
                smoothingSeconds,controlForce,disturbance,instability,difficulty,balanceClamp,fallThreshold,fallGraceSeconds,lateralAcceleration,lateralDamping,weightTorque,rollDamping,rollRestoring,footingCoupling,stepTorque,windTorque,stepLength,progressDifficulty})
                if (float.IsNaN(v) || float.IsInfinity(v)) throw new ArgumentException("Rules must be finite.");
            if (countdownSeconds < 0 || length <= 0 || forwardSpeed <= 0 || maxSeconds < length / forwardSpeed
                || tiltDegrees <= 0 || deadZone < 0 || deadZone >= 1 || smoothingSeconds < 0 || controlForce <= 0
                || disturbance < 0 || instability < 0 || difficulty < 0 || balanceClamp <= 0
                || fallThreshold <= 0 || fallThreshold > balanceClamp || fallGraceSeconds <= 0 || freshnessMilliseconds < 1 || lateralAcceleration <= 0 || lateralDamping < 0 || weightTorque < 0
                || rollDamping < 0 || rollRestoring < 0 || footingCoupling < 0 || stepTorque < 0 || windTorque < 0
                || stepLength <= 0 || progressDifficulty < 0)
                throw new ArgumentException("Invalid Log Balance rules.");
        }
        public LogBalanceRules Copy() => (LogBalanceRules)MemberwiseClone();
    }
    public enum LogBalanceState { Ready, Countdown, Crossing, Won, Fell }

    // An attempt result is local gameplay data, not a durable backend publication or official ledger ACK.
    public sealed class LogBalanceResult
    {
        public int PlayerId { get; }
        public bool Completed { get; }
        public float Seconds { get; }
        public float Distance { get; }
        public float Progress { get; }
        public string Reason { get; }
        internal LogBalanceResult(int player, bool completed, float seconds, float distance, float progress, string reason)
        { PlayerId=player;Completed=completed;Seconds=seconds;Distance=distance;Progress=progress;Reason=reason; }
    }
    // Pure rules: fixed substeps make threshold dwell and outcome ordering independent of render cadence.
    public sealed class LogBalanceModel
    {
        readonly LogBalanceRules rules;
        float countdown, outsideSeconds, accumulator;
        int player;
        const float Step = .01f;
        public LogBalanceState State { get; private set; }
        public float Balance { get; private set; }
        public float FilteredTilt { get; private set; }
        public float Elapsed { get; private set; }
        public float LateralVelocity { get; private set; }
        public float LogAngle { get; private set; }
        public float LogAngularVelocity { get; private set; }
        public float ForwardVelocity { get; private set; }
        public float Wind { get; private set; }
        public float FootstepPhase => Distance / rules.stepLength * (float)Math.PI;
        public float EffectiveFallThreshold => rules.fallThreshold*(rules.coupledPhysics?1-.13f*Progress:1);
        public float Danger => Math.Min(1,Math.Abs(Balance)/EffectiveFallThreshold);
        public float Distance { get; private set; }
        public float Progress => Math.Min(1,Distance/rules.length);
        public float Countdown => Math.Max(0,countdown);
        public LogBalanceResult Result { get; private set; }
        public LogBalanceModel(LogBalanceRules rules) { rules.Validate();this.rules=rules.Copy();Reset(); }
        public void Reset()
        { State=LogBalanceState.Ready;Balance=FilteredTilt=Elapsed=Distance=outsideSeconds=accumulator=0;countdown=rules.countdownSeconds;LateralVelocity=LogAngle=LogAngularVelocity=ForwardVelocity=Wind=0;Result=null;player=0; }
        public void Begin(int playerId)
        { if(playerId<1||playerId>4)throw new ArgumentOutOfRangeException(nameof(playerId));Reset();player=playerId;State=LogBalanceState.Countdown; }
        public void Tick(float seconds, float tilt)
        {
            if(float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<0||seconds>1)throw new ArgumentOutOfRangeException(nameof(seconds));
            if(float.IsNaN(tilt)||float.IsInfinity(tilt))throw new ArgumentOutOfRangeException(nameof(tilt));
            if(State!=LogBalanceState.Countdown&&State!=LogBalanceState.Crossing)return;
            accumulator+=seconds;
            while(accumulator+0.000001f>=Step && Result==null)
            {
                accumulator-=Step;
                if(State==LogBalanceState.Countdown)
                { countdown-=Step;if(countdown<=0.00001f){countdown=0;State=LogBalanceState.Crossing;}continue; }
                float target=Math.Max(-1,Math.Min(1,tilt));
                target=Math.Abs(target)<=rules.deadZone?0:Math.Sign(target)*(Math.Abs(target)-rules.deadZone)/(1-rules.deadZone);
                float blend=rules.smoothingSeconds==0?1:1-(float)Math.Exp(-Step/rules.smoothingSeconds);
                FilteredTilt+=(target-FilteredTilt)*blend;
                if(rules.coupledPhysics) SimulateCoupled();
                else
                {
                    float perturbation=rules.difficulty*(rules.disturbance+rules.instability*Balance);
                    Balance=Clamp(Balance+(perturbation+FilteredTilt*rules.controlForce)*Step,-rules.balanceClamp,rules.balanceClamp);
                    ForwardVelocity=rules.forwardSpeed;
                }
                Elapsed+=Step;Distance=Math.Min(rules.length,Distance+ForwardVelocity*Step);
                // Recovering lowers accumulated danger rather than erasing it with a tiny edge crossing.
                if(Math.Abs(Balance)>=EffectiveFallThreshold)outsideSeconds+=Step;
                else outsideSeconds=rules.coupledPhysics?Math.Max(0,outsideSeconds-Step*2):0;
                float grace=rules.fallGraceSeconds*(rules.coupledPhysics?1-.25f*Progress:1);
                // Falling wins the tie if the grace threshold and goal are crossed on the same simulation step.
                if(outsideSeconds+0.00001f>=grace)Finish(false,"balance");
                else if(Distance+0.00001f>=rules.length)Finish(true,"goal");
                else if(Elapsed>=rules.maxSeconds)Finish(false,"timeout");
            }
        }
        static float Clamp(float value,float min,float max)=>Math.Max(min,Math.Min(max,value));
        void SimulateCoupled()
        {
            float ramp=1+rules.progressDifficulty*Progress;
            // Deterministic gusts and footfalls are shared by every player; no random death impulses.
            Wind=rules.windTorque*rules.difficulty*ramp*(float)(Math.Sin(Elapsed*.83)+.35*Math.Sin(Elapsed*1.71));
            float steps=rules.stepTorque*rules.difficulty*ramp*(float)Math.Sin(FootstepPhase);
            float torque=rules.weightTorque*Balance + steps + Wind - .25f*FilteredTilt
                - rules.rollRestoring*LogAngle-rules.rollDamping*LogAngularVelocity;
            LogAngularVelocity=Clamp(LogAngularVelocity+torque*Step,-2,2);
            LogAngle=Clamp(LogAngle+LogAngularVelocity*Step,-1.15f,1.15f);
            if(Math.Abs(LogAngle)>=1.15f&&Math.Sign(LogAngularVelocity)==Math.Sign(LogAngle))LogAngularVelocity=0;
            float acceleration=FilteredTilt*rules.controlForce*rules.lateralAcceleration
                +rules.footingCoupling*(float)Math.Sin(LogAngle)+.35f*LogAngularVelocity
                +rules.difficulty*ramp*(rules.disturbance+rules.instability*Balance)
                -rules.lateralDamping*LateralVelocity;
            LateralVelocity=Clamp(LateralVelocity+acceleration*Step,-3,3);
            Balance=Clamp(Balance+LateralVelocity*Step,-rules.balanceClamp,rules.balanceClamp);
            if(Math.Abs(Balance)>=rules.balanceClamp&&Math.Sign(LateralVelocity)==Math.Sign(Balance))LateralVelocity=0;
            // Short, cautious steps near the edge cost time; centering restores the normal pace.
            ForwardVelocity=rules.forwardSpeed*Clamp(1-.48f*Danger*Danger-.12f*Math.Abs(LogAngularVelocity),.42f,1);
        }
        void Finish(bool completed,string reason)
        { State=completed?LogBalanceState.Won:LogBalanceState.Fell;Result=new LogBalanceResult(player,completed,Elapsed,Distance,Progress,reason); }
    }
}
