using System;
using GorillaEscape.Contracts;
using GorillaEscape.Input;
using GorillaEscape.Minigames.LogBalance;
using NUnit.Framework;

public sealed class LogBalanceTests
{
    static LogBalanceRules Quiet() => new LogBalanceRules {countdownSeconds=0,disturbance=0,instability=0};
    static LogBalanceModel Started(LogBalanceRules rules=null)
    {var m=new LogBalanceModel(rules??Quiet());m.Begin(1);m.Tick(.02f,0);return m;}
    static void Run(LogBalanceModel m,float seconds,float input=0)
    {for(int i=0;i<(int)Math.Round(seconds/.02f);i++)m.Tick(.02f,input);}
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
        {var r=new LogBalanceRules{countdownSeconds=0};var m=Started(r);for(int i=0;i<32/dt&&m.Result==null;i++)m.Tick(dt,-.2f-m.Balance*.8f);Assert.That(m.State,Is.EqualTo(LogBalanceState.Won),"dt="+dt);}
    }
    [Test] public void InvalidRulesAndNonFiniteInputsFailExplicitly(){var r=Quiet();r.deadZone=1;Assert.Throws<ArgumentException>(()=>new LogBalanceModel(r));var m=Started();Assert.Throws<ArgumentOutOfRangeException>(()=>m.Tick(.1f,float.NaN));Assert.Throws<ArgumentOutOfRangeException>(()=>m.Tick(float.NaN,0));}
    static PhoneInputWire Sample(float gamma=10,long seq=1,long epoch=1) => new PhoneInputWire {
        playerId=1,sequence=seq,connectionEpoch=epoch,sessionId="session",deviceSessionId="device",messageType="MOTION_SAMPLE",connectionState="ACTIVE",serverReceiveTimestamp=1000,
        quality=new PhoneQuality{source="synthetic",status="available"},orientation=new PhoneValues{availability="partial",hasGamma=true,gamma=gamma},screenOrientation=new PhoneValues{availability="present",hasAngle=true,angle=0}};
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
