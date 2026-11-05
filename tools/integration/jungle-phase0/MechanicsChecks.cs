using System;
using System.Collections.Generic;
using GorillaEscape.Gameplay.Input;
static class MechanicsChecks
{
 static void Require(bool pass){if(!pass)throw new Exception("mechanics assertion failed");}
 public static IEnumerable<(string,Action)> All(){
  yield return ("Smash and throw are deterministic for identical matched inputs",()=>{int score=CompetitiveMechanics.SmashScore(2,380,.45f,.9f);float power=CompetitiveMechanics.ThrowPower(2,380,43,0,.9f);for(int i=0;i<100;i++){Require(score==CompetitiveMechanics.SmashScore(2,380,.45f,.9f));Require(power==CompetitiveMechanics.ThrowPower(2,380,43,0,.9f));}});
  yield return ("Smash full technique can reach the top of the tower",()=>Require(CompetitiveMechanics.SmashScore(2.6f,420,.5f,1)==10000));
  yield return ("Stronger, centered and synchronized technique never scores worse",()=>{Require(CompetitiveMechanics.SmashScore(2.6f,420,.5f,1)>CompetitiveMechanics.SmashScore(.5f,80,.5f,1));Require(CompetitiveMechanics.SmashScore(2,380,.5f,1)>CompetitiveMechanics.SmashScore(2,380,0,0));Require(CompetitiveMechanics.ThrowPower(2,380,43,0,1)>CompetitiveMechanics.ThrowPower(2,380,20,14,0));});
  yield return ("Timing quality uses sensor timestamps and rejects absent clocks",()=>{Require(CompetitiveMechanics.TimingQuality(1000,1000)==1);Require(Math.Abs(CompetitiveMechanics.TimingQuality(1050,1000)-.75f)<1e-6);Require(CompetitiveMechanics.TimingQuality(0,1000)==0);Require(CompetitiveMechanics.TimingQuality(long.MaxValue,1)==0);});
  foreach(int fps in new[]{10,15,30,60,120,240}){int cadence=fps;yield return ($"Coconut landing is exact at {cadence} FPS",()=>{double hit=CompetitiveMechanics.GroundTime(1.2,18,.25,-9.81),elapsed=0;while(elapsed<hit)elapsed=Math.Min(hit,elapsed+1d/cadence);Require(Math.Abs(CompetitiveMechanics.Position(1.2,18,-9.81,elapsed)-.25)<1e-8);Require(Math.Abs(CompetitiveMechanics.Position(.2,15,0,elapsed)-(.2+15*hit))<1e-8);});}
  yield return ("Baseball's displayed distance agrees with the physical landing",()=>{const float time=2.7f;foreach(float distance in new[]{25f,72f,105f,135f}){float vy=CompetitiveMechanics.VerticalVelocityForLanding(1.4f,-.04f,-9.81f,time);Require(Math.Abs(CompetitiveMechanics.Position(1.4,vy,-9.81f,time)+.04)<1e-5);Require(Math.Abs(CompetitiveMechanics.Position(0,distance/time,0,time)-distance)<1e-5);}});
  yield return ("Malformed sporting inputs fail instead of producing a score",()=>{bool rejected=false;try{CompetitiveMechanics.ThrowPower(float.NaN,400,43,0,1);}catch(ArgumentException){rejected=true;}Require(rejected);});
 }
}
