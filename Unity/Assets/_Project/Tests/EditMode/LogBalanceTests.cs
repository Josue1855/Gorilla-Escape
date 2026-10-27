using System;
using System.Collections.Generic;
using GorillaEscape.Contracts;
using GorillaEscape.Input;
using GorillaEscape.Minigames.LogBalance;
using NUnit.Framework;

public sealed class LogBalanceTests
{
    static LogBalanceRules Quiet() => new LogBalanceRules {countdownSeconds=0,disturbance=0,instability=0,coupledPhysics=false};
    static LogBalanceModel Started(LogBalanceRules rules=null)
    {var m=new LogBalanceModel(rules??Quiet());m.Begin(1);m.Tick(.02f,0);return m;}
    static void Run(LogBalanceModel m,float seconds,float input=0)
    {for(int i=0;i<(int)Math.Round(seconds/.02f);i++)m.Tick(.02f,input);}
    [Test] public void ProgressiveDifficultyStartsGentleAndIncreasesContinuously()
    {
        float prior=LogBalanceModel.DifficultyAt(0,.65f);
        Assert.That(prior,Is.EqualTo(.65f).Within(.001f));
        for(int i=1;i<=100;i++){
            float current=LogBalanceModel.DifficultyAt(i/100f,.65f);
            Assert.That(current,Is.GreaterThanOrEqualTo(prior));
            Assert.That(current-prior,Is.LessThan(.02f));prior=current;
        }
        Assert.That(prior,Is.EqualTo(1.65f).Within(.001f));
        Assert.That(LogBalanceModel.DifficultyAt(.5f,0),Is.EqualTo(1));
    }
    [Test] public void SustainedBadInputWarnsBeforeFalling()
    {
        var m=Started(new LogBalanceRules{countdownSeconds=0});float warned=-1;
        for(int i=0;i<1000&&m.Result==null;i++){
            m.Tick(.02f,1);
            if(m.Danger>=.6f&&warned<0)warned=m.Elapsed;
        }
        Assert.That(m.State,Is.EqualTo(LogBalanceState.Fell));
        Assert.That(warned,Is.GreaterThanOrEqualTo(0));
        Assert.That(m.Elapsed-warned,Is.GreaterThan(.3f));
    }
    [Test] public void NeutralRemainsBalancedWithoutDisturbance(){var m=Started();Run(m,2);Assert.That(m.Balance,Is.Zero);Assert.That(m.Distance,Is.GreaterThan(0));}
    [TestCase(-1,-1)] [TestCase(1,1)]
    public void TiltMovesInRequestedDirection(float tilt,int sign){var m=Started();Run(m,.3f,tilt);Assert.That(Math.Sign(m.Balance),Is.EqualTo(sign));}
    [Test] public void DeadZoneRejectsSmallNoise(){var m=Started();Run(m,1,.07f);Assert.That(m.FilteredTilt,Is.Zero);Assert.That(m.Balance,Is.Zero);}
    [Test] public void SmoothingReducesStepAndConverges(){var m=Started();m.Tick(.02f,1);float initial=m.FilteredTilt;Assert.That(initial,Is.InRange(.01f,.2f));Run(m,.4f,1);Assert.That(m.FilteredTilt,Is.GreaterThan(initial).And.LessThan(1));}
    [Test] public void BalanceIsClampedAndFallRequiresSustainedThreshold(){var r=Quiet();r.fallGraceSeconds=2;var m=Started(r);Run(m,1.5f,1);Assert.That(m.Balance,Is.EqualTo(1));Assert.That(m.State,Is.EqualTo(LogBalanceState.Crossing));Run(m,2,1);Assert.That(m.State,Is.EqualTo(LogBalanceState.Fell));}
    [Test] public void RecoveringInsideLimitClearsFallDwell(){var r=Quiet();r.smoothingSeconds=0;r.fallGraceSeconds=.5f;var m=Started(r);Run(m,1.1f,1);Run(m,.3f,-1);Assert.That(m.Balance,Is.LessThan(r.fallThreshold));Run(m,.25f,1);Assert.That(m.Result,Is.Null);}
    [Test] public void GoalProducesImmutableResultAndTerminalTicksCannotFall(){var m=Started();Run(m,25);Assert.That(m.State,Is.EqualTo(LogBalanceState.Won));Assert.That(m.Result.Completed,Is.True);Assert.That(m.Result.Progress,Is.EqualTo(1));var result=m.Result;Run(m,3,1);Assert.That(m.Result,Is.SameAs(result));Assert.That(m.State,Is.EqualTo(LogBalanceState.Won));}
    [Test] public void FallCannotLaterBecomeVictory(){var m=Started();Run(m,4,1);Assert.That(m.State,Is.EqualTo(LogBalanceState.Fell));var result=m.Result;Run(m,30);Assert.That(m.Result,Is.SameAs(result));Assert.That(m.Result.Completed,Is.False);}
    [Test] public void ResetClearsAttemptAndAllowsDifferentPlayer(){var m=Started();Run(m,4,1);m.Reset();Assert.That(m.Result,Is.Null);Assert.That(m.State,Is.EqualTo(LogBalanceState.Ready));Assert.That(m.Elapsed,Is.Zero);Assert.That(m.Distance,Is.Zero);Assert.That(m.Balance,Is.Zero);Assert.That(m.FilteredTilt,Is.Zero);m.Begin(3);Run(m,25);Assert.That(m.Result.PlayerId,Is.EqualTo(3));}
    [Test] public void CountdownDoesNotAdvanceOrApplyTilt(){var m=new LogBalanceModel(new LogBalanceRules());m.Begin(1);Run(m,2,1);Assert.That(m.State,Is.EqualTo(LogBalanceState.Countdown));Assert.That(m.Distance,Is.Zero);Assert.That(m.Balance,Is.Zero);}
    [Test] public void DefaultDisturbanceEventuallyRequiresCorrection(){var m=Started(new LogBalanceRules{countdownSeconds=0});Run(m,25);Assert.That(m.State,Is.EqualTo(LogBalanceState.Fell));Assert.That(m.Elapsed,Is.LessThan(24));}
    [Test] public void FeedbackCanWinDefaultRulesAtDifferentFrameRates()
    {
        foreach(float dt in new[]{.01f,.02f,.05f})
        {var r=new LogBalanceRules{countdownSeconds=0};var m=Started(r);for(int i=0;i<32/dt&&m.Result==null;i++)m.Tick(dt,-m.Balance*1.9f-m.LateralVelocity*.85f-m.LogAngle*.8f-m.LogAngularVelocity*.25f-.04f);Assert.That(m.State,Is.EqualTo(LogBalanceState.Won),"dt="+dt);}
    }
    [TestCase(.01f)] [TestCase(.02f)] [TestCase(.05f)]
    public void MomentumFeedbackCanFinishWithQuarterSecondDelayAndLimitedTiltRate(float dt)
    {
        var m=Started(new LogBalanceRules{countdownSeconds=0});var pending=new Queue<float>();float input=0;
        for(int i=0;i<40/dt&&m.Result==null;i++){
            float target=Math.Max(-1,Math.Min(1,-m.Balance*1.9f-m.LateralVelocity*.85f-m.LogAngle*.8f-m.LogAngularVelocity*.25f-.04f));
            pending.Enqueue(target);target=pending.Count>Math.Round(.25f/dt)?pending.Dequeue():0;
            input+=Math.Max(-1.5f*dt,Math.Min(1.5f*dt,target-input));m.Tick(dt,input);
        }
        Assert.That(m.State,Is.EqualTo(LogBalanceState.Won));
    }
    [Test] public void InvalidRulesAndNonFiniteInputsFailExplicitly(){var r=Quiet();r.deadZone=1;Assert.Throws<ArgumentException>(()=>new LogBalanceModel(r));var m=Started();Assert.Throws<ArgumentOutOfRangeException>(()=>m.Tick(.1f,float.NaN));Assert.Throws<ArgumentOutOfRangeException>(()=>m.Tick(float.NaN,0));}
    [Test] public void CountersteeringBrakesMomentumInsteadOfTeleportingAcrossCenter()
    {
        var r=new LogBalanceRules{countdownSeconds=0,disturbance=0,instability=0,windTorque=0,stepTorque=0};
        var m=Started(r);Run(m,.4f,1);float before=m.Balance,velocity=m.LateralVelocity;
        Run(m,.08f,0);Assert.That(m.Balance,Is.GreaterThan(before));Assert.That(m.LateralVelocity,Is.GreaterThan(0));
        Run(m,.2f,-1);Assert.That(m.LateralVelocity,Is.LessThan(velocity));
    }
    [Test] public void WeightTurnsLogToTheSideOccupiedByPlayer()
    {
        foreach(int direction in new[]{-1,1}){
            var r=new LogBalanceRules{countdownSeconds=0,disturbance=0,instability=0,windTorque=0,stepTorque=0};
            var m=Started(r);Run(m,1,direction);
            Assert.That(Math.Sign(m.LogAngle),Is.EqualTo(direction));
        }
    }
    [Test] public void ForwardFootfallsGenerateRollWithoutAnElapsedTimeMotor()
    {
        var r=new LogBalanceRules{countdownSeconds=0,disturbance=0,instability=0,windTorque=0,weightTorque=0};
        var moving=Started(r);Run(moving,1);Assert.That(Math.Abs(moving.LogAngle),Is.GreaterThan(.001f));
        r.stepTorque=0;var quiet=Started(r);Run(quiet,1);Assert.That(quiet.LogAngle,Is.Zero);
    }
    [Test] public void HoldingOneCorrectionIsNotEnoughAndProgressAddsDifficulty()
    {
        var m=Started(new LogBalanceRules{countdownSeconds=0});Run(m,30,-.2f);
        Assert.That(m.State,Is.EqualTo(LogBalanceState.Fell));
        Assert.That(m.EffectiveFallThreshold,Is.LessThan(.85f));
    }
    [Test] public void CoupledSimulationIsDeterministicAcrossRenderCadencesAndResetClearsMomentum()
    {
        var a=Started(new LogBalanceRules{countdownSeconds=0});var b=Started(new LogBalanceRules{countdownSeconds=0});
        for(int i=0;i<200;i++)a.Tick(.01f,.1f);
        for(int i=0;i<40;i++)b.Tick(.05f,.1f);
        Assert.That(a.Balance,Is.EqualTo(b.Balance).Within(.00001));Assert.That(a.LogAngle,Is.EqualTo(b.LogAngle).Within(.00001));
        Assert.That(a.Distance,Is.EqualTo(b.Distance).Within(.00001));a.Reset();
        Assert.That(a.LateralVelocity,Is.Zero);Assert.That(a.LogAngle,Is.Zero);Assert.That(a.LogAngularVelocity,Is.Zero);
    }
    static PhoneInputWire Sample(float gamma=10,long seq=1,long epoch=1) => new PhoneInputWire {
        playerId=1,sequence=seq,connectionEpoch=epoch,sessionId="session",deviceSessionId="device",messageType="MOTION_SAMPLE",connectionState="ACTIVE",serverReceiveTimestamp=1000,
        quality=new PhoneQuality{source="synthetic",status="available"},orientation=new PhoneValues{availability="partial",hasGamma=true,gamma=gamma},screenOrientation=new PhoneValues{availability="present",hasAngle=true,angle=0}};
    [Test] public void AutomaticNeutralNeedsDistinctStableSamplesAndKeepsItsReferenceDuringPlay()
    {
        var store=new PhoneInputStore();var adapter=new LogBalanceTilt();var rules=Quiet();
        for(int i=0;i<11;i++){
            var sample=Sample(12+(i%2)*.3f,i+1);sample.serverReceiveTimestamp=1000+i*50;store.Accept(sample);
            bool ready=adapter.PrepareAutomatically(store,1,sample.serverReceiveTimestamp,rules);
            Assert.That(ready,Is.EqualTo(i>=9));
        }
        Assert.True(adapter.TryTilt(store,1,1500,rules,out float neutral));Assert.That(Math.Abs(neutral),Is.LessThan(.02));
        var leaning=Sample(37,12);leaning.serverReceiveTimestamp=1550;store.Accept(leaning);
        Assert.True(adapter.TryTilt(store,1,1550,rules,out float input));Assert.That(input,Is.GreaterThan(.95f));
    }
    [Test] public void AutomaticNeutralCannotUseOneRepeatedOrStaleSample()
    {
        var store=new PhoneInputStore();store.Accept(Sample());var adapter=new LogBalanceTilt();
        for(int i=0;i<20;i++)Assert.False(adapter.PrepareAutomatically(store,1,1000+i*50,Quiet()));
        Assert.False(adapter.Calibrated);
    }
    [Test] public void AutomaticNeutralWaitsThroughMovementAndRelearnsAfterReconnect()
    {
        var store=new PhoneInputStore();var adapter=new LogBalanceTilt();var rules=Quiet();
        for(int i=0;i<15;i++){
            var sample=Sample(i*3,i+1);sample.serverReceiveTimestamp=1000+i*50;store.Accept(sample);
            Assert.False(adapter.PrepareAutomatically(store,1,sample.serverReceiveTimestamp,rules));
        }
        for(int i=0;i<12;i++){
            var sample=Sample(-15,i+1,2);sample.serverReceiveTimestamp=2000+i*50;store.Accept(sample);
            adapter.PrepareAutomatically(store,1,sample.serverReceiveTimestamp,rules);
        }
        Assert.True(adapter.Calibrated);Assert.True(adapter.TryTilt(store,1,2550,rules,out float value));Assert.That(value,Is.Zero);
    }
    [Test] public void PhoneUsesNeutralAndExistingPlayerStore(){var store=new PhoneInputStore();store.Accept(Sample());var adapter=new LogBalanceTilt();var rules=Quiet();Assert.True(adapter.Calibrate(store,1,1000,rules));Assert.True(adapter.TryTilt(store,1,1000,rules,out float value));Assert.That(value,Is.Zero);store.Accept(Sample(35,2));Assert.True(adapter.TryTilt(store,1,1000,rules,out value));Assert.That(value,Is.EqualTo(1));store.Accept(Sample(-15,3));Assert.True(adapter.TryTilt(store,1,1000,rules,out value));Assert.That(value,Is.EqualTo(-1));Assert.False(adapter.TryTilt(store,2,1000,rules,out _));}
    [Test] public void StaleDisconnectedAndLandscapeSamplesNeverDriveAttempt()
    {
        var store=new PhoneInputStore();store.Accept(Sample());var adapter=new LogBalanceTilt();var r=Quiet();Assert.True(adapter.Calibrate(store,1,1000,r));Assert.False(adapter.TryTilt(store,1,1400,r,out _));
        var landscape=Sample(20,2);landscape.screenOrientation.angle=90;store.Accept(landscape);Assert.False(adapter.TryTilt(store,1,1000,r,out _));
        var disconnect=Sample(20,3);disconnect.connectionState="DISCONNECTED";store.Accept(disconnect);Assert.False(adapter.TryTilt(store,1,1000,r,out _));
    }
    [Test] public void ReconnectInvalidatesCalibrationAndRequiresExplicitNeutral()
    {var store=new PhoneInputStore();store.Accept(Sample());var adapter=new LogBalanceTilt();var r=Quiet();adapter.Calibrate(store,1,1000,r);store.Accept(Sample(10,1,2));Assert.False(adapter.TryTilt(store,1,1000,r,out _));Assert.False(adapter.Calibrated);Assert.True(adapter.Calibrate(store,1,1000,r));Assert.True(adapter.TryTilt(store,1,1000,r,out _));}
    [Test] public void MissingAndNonFiniteOrientationCannotCalibrate()
    {foreach(float gamma in new[]{float.NaN,float.PositiveInfinity,91f}){var store=new PhoneInputStore();store.Accept(Sample(gamma));Assert.False(new LogBalanceTilt().Calibrate(store,1,1000,Quiet()));}var absent=Sample();absent.orientation.hasGamma=false;var s=new PhoneInputStore();s.Accept(absent);Assert.False(new LogBalanceTilt().Calibrate(s,1,1000,Quiet()));}
}
