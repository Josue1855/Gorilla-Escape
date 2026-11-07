using System;
using GorillaEscape.Contracts;
using GorillaEscape.Input.Fusion;
using GorillaEscape.Gameplay.Input;
using NUnit.Framework;
namespace GorillaEscape.Tests {
 public sealed class CameraGameplayControlsTests {
  static CameraInputWire Camera(long time=1000,long seq=1) {
   var names=new[]{"left-shoulder","right-shoulder","left-elbow","right-elbow","left-wrist","right-wrist"};
   var points=Array.ConvertAll(names,n=>new CameraLandmark{name=n,x=.4f,y=.3f,visible=true,visibility=.9f,presence=.9f});
   return new CameraInputWire{version=1,width=640,height=480,frameSequence=seq,captureTimestamp=time,processTimestamp=time+1,
    clockDomain="pc-unix-ms",captureTimeSource="fixture",source="synthetic",quality="fixture",trackingState="TRACKING",
    subjects=new[]{new CameraSubject{subjectId=1,centerX=.5f,centerY=.5f,confidence=.9f,landmarks=points}}};
  }
  [TestCase(JungleGame.Boxing, JungleAction.PunchRight)]
  [TestCase(JungleGame.Drums, JungleAction.DrumRight)]
  public void CameraGamesAuthorizeDownwardStrokeWithoutAPhone(JungleGame game,JungleAction action) {
   var input=new JungleGameplayInput();input.Select(game,true);
   for(int i=0;i<3;i++) {var f=Camera(time:1000+i*50,seq:i+1);input.UpdateCamera(f,PlayerLockState.LOCKED,1,1001+i*50);}
   var hit=Camera(time:1150,seq:4);Array.Find(hit.subjects[0].landmarks,l=>l.name=="right-wrist").y+=.08f;
   input.UpdateCamera(hit,PlayerLockState.LOCKED,1,1151);
   Assert.That(input.Ready,Is.True);Assert.That(input.Authorize(action,input.Current.Revision,1151),Is.True);
   Assert.That(input.Authorize(action,input.Current.Revision,1151),Is.False);
   Assert.That(input.Authorize(action,input.Current.Revision,1500),Is.False);
  }
  [Test] public void BaseballRequiresHorizontalSwingAndRebasesAfterTrackingLoss() {
   var input=new JungleGameplayInput();input.Select(JungleGame.Baseball,true);
   for(int i=0;i<3;i++)input.UpdateCamera(Camera(time:1000+i*50,seq:i+1),PlayerLockState.LOCKED,1,1001+i*50);
   var hit=Camera(time:1150,seq:4);Array.Find(hit.subjects[0].landmarks,l=>l.name=="right-wrist").x+=.08f;
   input.UpdateCamera(hit,PlayerLockState.LOCKED,1,1151);
   Assert.That(input.Authorize(JungleAction.Swing,input.Current.Revision,1151),Is.True);
   input.UpdateCamera(null,PlayerLockState.LOST,1,1200);
   input.UpdateCamera(Camera(time:1300,seq:5),PlayerLockState.LOCKED,1,1301);
   Assert.That(input.Current.Baseline,Is.True);Assert.That(input.Current.Actions,Is.EqualTo(JungleAction.None));
  }
  [TestCase(JungleGame.Boxing)][TestCase(JungleGame.Drums)]
  public void TwoHandCameraGamesPauseWhenEitherHandDisappears(JungleGame game) {
   var input=new JungleGameplayInput();input.Select(game,true);
   var f=Camera();Array.Find(f.subjects[0].landmarks,l=>l.name=="left-wrist").visible=false;
   input.UpdateCamera(f,PlayerLockState.LOCKED,1,1001);Assert.That(input.Ready,Is.False);
  }

 }
}
