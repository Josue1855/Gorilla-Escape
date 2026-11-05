using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using GorillaEscape.Gameplay.Input;
public sealed class CompetitiveMechanicsTests
{
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 [TestCase(10)][TestCase(15)][TestCase(30)][TestCase(60)][TestCase(120)][TestCase(240)]
 public void LandingDoesNotDependOnFrameRate(int fps){double at=CompetitiveMechanics.GroundTime(1.2,18,.25,-9.81),clock=0;while(clock<at)clock=Math.Min(at,clock+1d/fps);Assert.That(CompetitiveMechanics.Position(1.2,18,-9.81,clock),Is.EqualTo(.25).Within(1e-8));}
 [Test] public void MatchedStrengthAndTimingScoreRepeatably(){Assert.That(CompetitiveMechanics.SmashScore(2.6f,420,.5f,1),Is.EqualTo(10000));Assert.That(CompetitiveMechanics.TimingQuality(1000,1100),Is.EqualTo(.5));}
 [Test] public void BowlingCurveUsesTheBallPositionRatherThanControllerOrigin(){
  var host=new GameObject("Inactive bowling rules fixture");host.SetActive(false);var ball=new GameObject("Physics ball");
  try{var type=Type.GetType("BowlingController, Assembly-CSharp",true);var controller=host.AddComponent(type);var body=ball.AddComponent<Rigidbody>();body.useGravity=false;type.GetField("ballBody",Flags).SetValue(controller,body);type.GetField("bowlingSpin",Flags).SetValue(controller,1f);var state=type.GetField("state",Flags);state.SetValue(controller,Enum.Parse(state.FieldType,"Rolling"));var tick=type.GetMethod("FixedUpdate",Flags);
   body.position=new Vector3(0,0,20);tick.Invoke(controller,null);Assert.That(body.GetAccumulatedForce(),Is.EqualTo(Vector3.zero));
   body.position=new Vector3(0,0,5);tick.Invoke(controller,null);Assert.That(body.GetAccumulatedForce().x,Is.GreaterThan(0));
  }finally{UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(ball);}
 }
 [Test] public void FirstPitchIsNotDisplayedAsSecondPitch(){
  var host=new GameObject("Inactive baseball display fixture");host.SetActive(false);var nodes=new System.Collections.Generic.List<JungleRetainedUI.Node>();var root=new VisualElement();
  try{var type=Type.GetType("BaseballController, Assembly-CSharp",true);var controller=host.AddComponent(type);type.GetField("pitches",Flags).SetValue(controller,1);var state=type.GetField("state",Flags);state.SetValue(controller,Enum.Parse(state.FieldType,"Pitching"));JungleRetainedUI.Begin(root,nodes);type.GetMethod("RenderUI").Invoke(controller,null);JungleRetainedUI.End();Assert.That(root.Query<Label>().ToList().Exists(label=>label.text.StartsWith("LANZAMIENTO 1/10")),Is.True);
  }finally{UnityEngine.Object.DestroyImmediate(host);}
 }
}
