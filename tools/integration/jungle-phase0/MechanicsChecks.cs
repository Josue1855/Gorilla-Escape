using System;
using System.Collections.Generic;
using GorillaEscape.Gameplay.Input;
static class MechanicsChecks
{
 static void Require(bool pass){if(!pass)throw new Exception("mechanics assertion failed");}
 public static IEnumerable<(string,Action)> All(){
  yield return ("Smash power comes only from the phone, never camera speed",()=>Require(CompetitiveMechanics.SmashScore(.5f,300,.5f,1)==CompetitiveMechanics.SmashScore(10,300,.5f,1)));
  yield return ("Smash misses score zero, full technique reaches 10000",()=>{Require(CompetitiveMechanics.SmashImpactScore(420,0,1)==0);Require(CompetitiveMechanics.SmashImpactScore(420,1,1)==10000);});
  yield return ("Smash accuracy and power independently improve the result",()=>{Require(CompetitiveMechanics.SmashImpactScore(300,1,1)>CompetitiveMechanics.SmashImpactScore(300,.2f,1));Require(CompetitiveMechanics.SmashImpactScore(420,1,1)>CompetitiveMechanics.SmashImpactScore(180,1,1));});
  yield return ("Whack rewards controlled phone power and caps power and combo",()=>{Require(CompetitiveMechanics.WhackScore(420,1,1)>CompetitiveMechanics.WhackScore(180,1,1));Require(CompetitiveMechanics.WhackScore(10000,1,1000)==260);Require(CompetitiveMechanics.WhackScore(300,1,1)>CompetitiveMechanics.WhackScore(300,0,1));});
  yield return ("Device profiles preserve Whack and Smash phone power",()=>{Require(JungleGameplayInput.RequiresPhone(JungleGame.Whack)&&JungleGameplayInput.RequiresPhone(JungleGame.GorillaSmash));foreach(var game in new[]{JungleGame.Boxing,JungleGame.Baseball,JungleGame.Drums,JungleGame.CoconutSlice})Require(!JungleGameplayInput.RequiresPhone(game));});
  yield return ("Baseball timing is centered at the plate in seconds",()=>{Require(CompetitiveMechanics.BaseballTiming(-3.2f,-3.2f,6.4f)==1);Require(Math.Abs(CompetitiveMechanics.BaseballTiming(-3.2f+.12f*6.4f,-3.2f,6.4f)-.5f)<1e-5);Require(CompetitiveMechanics.BaseballTiming(5.8f,-3.2f,6.4f)==0);});
  foreach(var kind in new[]{JungleGame.Boxing,JungleGame.Drums,JungleGame.Baseball}) {
   var game=kind;
   yield return ($"{game}: camera gesture works without a phone and rejects stale replay",()=>{
    var input=new JungleGameplayInput();input.Select(game,true);
    for(int i=0;i<4;i++) {
     var camera=Checks.Camera(i+1);camera.captureTimestamp=1000+i*50;camera.processTimestamp=camera.captureTimestamp+1;
     if(i==3)foreach(var point in camera.subjects[0].landmarks)if(point.name=="right-wrist"){if(game==JungleGame.Baseball)point.x+=.08f;else point.y+=.08f;}
     input.UpdateCamera(camera,GorillaEscape.Input.Fusion.PlayerLockState.LOCKED,1,camera.captureTimestamp+1);
    }
    var action=game==JungleGame.Boxing?JungleAction.PunchRight:game==JungleGame.Drums?JungleAction.DrumRight:JungleAction.Swing;
    Require(input.Ready&&input.Authorize(action,input.Current.Revision,1151));Require(!input.Authorize(action,input.Current.Revision,1151));Require(!input.Authorize(action,input.Current.Revision,1500));
    input.UpdateCamera(null,GorillaEscape.Input.Fusion.PlayerLockState.LOST,1,1600);Require(!input.Ready);
   });
  }
  yield return ("Smash and throw are deterministic for identical matched inputs",()=>{int score=CompetitiveMechanics.SmashScore(2,380,.45f,.9f);float power=CompetitiveMechanics.ThrowPower(2,380,43,0,.9f);for(int i=0;i<100;i++){Require(score==CompetitiveMechanics.SmashScore(2,380,.45f,.9f));Require(power==CompetitiveMechanics.ThrowPower(2,380,43,0,.9f));}});
  yield return ("Smash full technique can reach the top of the tower",()=>Require(CompetitiveMechanics.SmashScore(2.6f,420,.5f,1)==10000));
  yield return ("Stronger, centered and synchronized technique never scores worse",()=>{Require(CompetitiveMechanics.SmashScore(2.6f,420,.5f,1)>CompetitiveMechanics.SmashScore(.5f,80,.5f,1));Require(CompetitiveMechanics.SmashScore(2,380,.5f,1)>CompetitiveMechanics.SmashScore(2,380,0,0));Require(CompetitiveMechanics.ThrowPower(2,380,43,0,1)>CompetitiveMechanics.ThrowPower(2,380,20,14,0));});
  yield return ("Timing quality uses sensor timestamps and rejects absent clocks",()=>{Require(CompetitiveMechanics.TimingQuality(1000,1000)==1);Require(Math.Abs(CompetitiveMechanics.TimingQuality(1050,1000)-.75f)<1e-6);Require(CompetitiveMechanics.TimingQuality(0,1000)==0);Require(CompetitiveMechanics.TimingQuality(long.MaxValue,1)==0);});
  foreach(int fps in new[]{10,15,30,60,120,240}){int cadence=fps;yield return ($"Coconut landing is exact at {cadence} FPS",()=>{double hit=CompetitiveMechanics.GroundTime(1.2,18,.25,-9.81),elapsed=0;while(elapsed<hit)elapsed=Math.Min(hit,elapsed+1d/cadence);Require(Math.Abs(CompetitiveMechanics.Position(1.2,18,-9.81,elapsed)-.25)<1e-8);Require(Math.Abs(CompetitiveMechanics.Position(.2,15,0,elapsed)-(.2+15*hit))<1e-8);});}
  yield return ("Baseball's displayed distance agrees with the physical landing",()=>{const float time=2.7f;foreach(float distance in new[]{25f,72f,105f,135f}){float vy=CompetitiveMechanics.VerticalVelocityForLanding(1.4f,-.04f,-9.81f,time);Require(Math.Abs(CompetitiveMechanics.Position(1.4,vy,-9.81f,time)+.04)<1e-5);Require(Math.Abs(CompetitiveMechanics.Position(0,distance/time,0,time)-distance)<1e-5);}});
  yield return ("Malformed sporting inputs fail instead of producing a score",()=>{bool rejected=false;try{CompetitiveMechanics.ThrowPower(float.NaN,400,43,0,1);}catch(ArgumentException){rejected=true;}Require(rejected);});
 }
}
