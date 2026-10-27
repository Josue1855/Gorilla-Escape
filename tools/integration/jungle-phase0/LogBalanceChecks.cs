using System;
using System.Collections.Generic;
using GorillaEscape.Gameplay.Input;
using GorillaEscape.Minigames.LogBalance;

static class LogBalanceChecks
{
    static void Require(bool value){if(!value)throw new Exception("assertion failed");}
    static LogBalanceRules Quiet()=>new LogBalanceRules{countdownSeconds=0,disturbance=0,instability=0,coupledPhysics=false};
    static LogBalanceModel Start(LogBalanceRules r=null){var m=new LogBalanceModel(r??Quiet());m.Begin(1);m.Tick(.02f,0);return m;}
    static void Run(LogBalanceModel m,float seconds,float tilt=0){for(int i=0;i<(int)Math.Round(seconds/.02f);i++)m.Tick(.02f,tilt);}
    static void Prepare(GameplayChecks.Lab lab,LogBalanceFusedControl control,float gamma=12)
    {for(int i=0;i<12;i++){lab.Step(1000+i*50,gamma:gamma);control.Observe(lab.Game.Current,Quiet(),true);}Require(control.Calibrated);}
    public static IEnumerable<(string,Action)> All()
    {
        yield return ("Tronco: neutral requires stable distinct fused phone samples",()=>{var l=new GameplayChecks.Lab(JungleGame.LogBalance);var c=new LogBalanceFusedControl();Prepare(l,c);Require(Math.Abs(c.Value)<.001);l.Step(1600,gamma:37);Require(c.Observe(l.Game.Current,Quiet(),false)&&Math.Abs(c.Value-1)<.001);});
        yield return ("Tronco: repeated frame cannot calibrate",()=>{var l=new GameplayChecks.Lab(JungleGame.LogBalance);l.Warm();var c=new LogBalanceFusedControl();for(int i=0;i<50;i++)c.Observe(l.Game.Current,Quiet(),true);Require(!c.Calibrated);});
        yield return ("Tronco: landscape orientation invalidates a calibrated control",()=>{var l=new GameplayChecks.Lab(JungleGame.LogBalance);var c=new LogBalanceFusedControl();Prepare(l,c);l.Step(1600,gamma:12,screen:90);Require(!c.Observe(l.Game.Current,Quiet(),false)&&!c.Calibrated&&c.Value==0);});
        yield return ("Tronco: source gaps restart the stability window",()=>{var l=new GameplayChecks.Lab(JungleGame.LogBalance);var c=new LogBalanceFusedControl();for(int i=0;i<6;i++){l.Step(1000+i*50,gamma:12);c.Observe(l.Game.Current,Quiet(),true);}l.Step(1700,gamma:12);c.Observe(l.Game.Current,Quiet(),true);Require(!c.Calibrated);});
        yield return ("Tronco: moving phone delays neutral calibration",()=>{var l=new GameplayChecks.Lab(JungleGame.LogBalance);var c=new LogBalanceFusedControl();for(int i=0;i<20;i++){l.Step(1000+i*50,gamma:i%2==0?12:20);c.Observe(l.Game.Current,Quiet(),true);}Require(!c.Calibrated);});
        yield return ("Tronco: no calibration while simulation is active",()=>{var l=new GameplayChecks.Lab(JungleGame.LogBalance);var c=new LogBalanceFusedControl();for(int i=0;i<20;i++){l.Step(1000+i*50);c.Observe(l.Game.Current,Quiet(),false);}Require(!c.Calibrated);});
        yield return ("Tronco: reacquisition resets neutral and rejects old authorization",()=>{var l=new GameplayChecks.Lab(JungleGame.LogBalance);var c=new LogBalanceFusedControl();Prepare(l,c);var old=l.Game.Current;l.Lost();Require(!l.Game.Authorize(JungleAction.Balance,old.Revision,l.At));l.Step(1700);l.Step(1750);Require(l.Game.Current.Baseline);c.Observe(l.Game.Current,Quiet(),true);Require(!c.Calibrated);});
        yield return ("Tronco: continuous balance is authorized once per valid revision",()=>{var l=new GameplayChecks.Lab(JungleGame.LogBalance);l.Warm();Require(l.Use(JungleAction.Balance)&&!l.Use(JungleAction.Balance)&&!l.Use(JungleAction.Throw));l.Step(1150);Require(l.Use(JungleAction.Balance));});
        yield return ("Tronco: quiet neutral advances without drift",()=>{var m=Start();Run(m,2);Require(m.Balance==0&&m.Distance>0);});
        foreach(int sign in new[]{-1,1}){int direction=sign;yield return ($"Tronco: tilt sign {sign}",()=>{var m=Start();Run(m,.3f,direction);Require(Math.Sign(m.Balance)==direction);});}
        yield return ("Tronco: dead zone suppresses sensor noise",()=>{var m=Start();Run(m,1,.07f);Require(m.FilteredTilt==0&&m.Balance==0);});
        yield return ("Tronco: smoothing limits abrupt corrections",()=>{var m=Start();m.Tick(.02f,1);float initial=m.FilteredTilt;Run(m,.4f,1);Require(initial>.01&&initial<.2&&m.FilteredTilt>initial&&m.FilteredTilt<1);});
        yield return ("Tronco: threshold needs sustained danger",()=>{var r=Quiet();r.fallGraceSeconds=2;var m=Start(r);Run(m,1.5f,1);Require(m.Balance==1&&m.State==LogBalanceState.Crossing);Run(m,2,1);Require(m.State==LogBalanceState.Fell);});
        yield return ("Tronco: countdown cannot advance or apply steering",()=>{var m=new LogBalanceModel(new LogBalanceRules());m.Begin(1);Run(m,2,1);Require(m.State==LogBalanceState.Countdown&&m.Distance==0&&m.Balance==0);});
        yield return ("Tronco: completed result is terminal",()=>{var m=Start();Run(m,25);Require(m.State==LogBalanceState.Won&&m.Result.Completed&&m.Result.Progress==1);var result=m.Result;Run(m,4,1);Require(ReferenceEquals(result,m.Result)&&m.State==LogBalanceState.Won);});
        yield return ("Tronco: fall cannot later become victory",()=>{var m=Start();Run(m,4,1);Require(m.State==LogBalanceState.Fell);var result=m.Result;Run(m,30);Require(ReferenceEquals(result,m.Result)&&!m.Result.Completed);});
        yield return ("Tronco: restart clears momentum and result",()=>{var m=Start(new LogBalanceRules{countdownSeconds=0});Run(m,2,1);m.Reset();Require(m.Result==null&&m.State==LogBalanceState.Ready&&m.Balance==0&&m.LateralVelocity==0&&m.LogAngle==0&&m.LogAngularVelocity==0);});
        yield return ("Tronco: invalid rules and nonfinite input fail explicitly",()=>{bool bad=false;try{new LogBalanceModel(new LogBalanceRules{deadZone=1});}catch(ArgumentException){bad=true;}Require(bad);bad=false;try{Start().Tick(.1f,float.NaN);}catch(ArgumentOutOfRangeException){bad=true;}Require(bad);});
        yield return ("Tronco: weight rolls the trunk toward the occupied side",()=>{foreach(int direction in new[]{-1,1}){var m=Start(new LogBalanceRules{countdownSeconds=0,disturbance=0,instability=0,windTorque=0,stepTorque=0});Run(m,1,direction);Require(Math.Sign(m.LogAngle)==direction);}});
        yield return ("Tronco: countersteering brakes existing momentum",()=>{var m=Start(new LogBalanceRules{countdownSeconds=0,disturbance=0,instability=0,windTorque=0,stepTorque=0});Run(m,.4f,1);float position=m.Balance,velocity=m.LateralVelocity;Run(m,.08f);Require(m.Balance>position&&m.LateralVelocity>0);Run(m,.2f,-1);Require(m.LateralVelocity<velocity);});
        yield return ("Tronco: fixed-step physics is independent of render cadence",()=>{var a=Start(new LogBalanceRules{countdownSeconds=0});var b=Start(new LogBalanceRules{countdownSeconds=0});for(int i=0;i<200;i++)a.Tick(.01f,.1f);for(int i=0;i<40;i++)b.Tick(.05f,.1f);Require(Math.Abs(a.Balance-b.Balance)<.00001&&Math.Abs(a.LogAngle-b.LogAngle)<.00001&&Math.Abs(a.Distance-b.Distance)<.00001);});
        foreach(float cadence in new[]{.01f,.02f,.05f}){float dt=cadence;yield return ($"Tronco: feedback can complete with 250 ms delay at dt={dt}",()=>{var m=Start(new LogBalanceRules{countdownSeconds=0});var pending=new Queue<float>();float input=0;for(int i=0;i<40/dt&&m.Result==null;i++){float target=Math.Max(-1,Math.Min(1,-m.Balance*1.9f-m.LateralVelocity*.85f-m.LogAngle*.8f-m.LogAngularVelocity*.25f-.04f));pending.Enqueue(target);target=pending.Count>Math.Round(.25f/dt)?pending.Dequeue():0;input+=Math.Max(-1.5f*dt,Math.Min(1.5f*dt,target-input));m.Tick(dt,input);}Require(m.State==LogBalanceState.Won);});}
    }
}
