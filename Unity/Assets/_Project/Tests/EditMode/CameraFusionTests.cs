using System;
using System.IO;
using System.Linq;
using GorillaEscape.Contracts;
using GorillaEscape.CameraTracking;
using GorillaEscape.Input;
using GorillaEscape.Input.Fusion;
using GorillaEscape.Gameplay.Input;
using NUnit.Framework;
using UnityEngine;
namespace GorillaEscape.Tests {
 public sealed class CameraFusionTests {
  [Test] public void ShortCameraGapDoesNotPauseButLongGapDoes(){
   var gate=new TrackingPauseStability();
   Assert.That(gate.Update(true,0,.18f),Is.True);
   Assert.That(gate.Update(true,.51f,.18f),Is.False);
   Assert.That(gate.Update(false,.6f,.18f),Is.False);
   Assert.That(gate.Update(true,.7f,.18f),Is.False);
   Assert.That(gate.Update(false,.8f,.18f),Is.False);
   Assert.That(gate.Update(false,1f,.18f),Is.True);
  }
  [Test] public void BadHandJumpDoesNotCancelOtherHandOrCreateRecoveredAction(){
   var input=new JungleGameplayInput();input.Select(JungleGame.CoconutSlice,true);
   input.UpdateCamera(Camera(time:1000,seq:1),PlayerLockState.LOCKED,1,1001);
   var bad=Camera(time:1010,seq:2);Array.Find(bad.subjects[0].landmarks,p=>p.name=="left-wrist").x=.95f;
   input.UpdateCamera(bad,PlayerLockState.LOCKED,1,1011);
   Assert.That(input.Ready,Is.True);Assert.That(input.Current.Pose.leftDetected,Is.False);
   Assert.That(input.Current.Pose.rightDetected,Is.True);Assert.That(input.Current.Actions&JungleAction.SliceLeft,Is.EqualTo(JungleAction.None));
   var recovered=Camera(time:1110,seq:3);input.UpdateCamera(recovered,PlayerLockState.LOCKED,1,1111);
   Assert.That(input.Ready,Is.True);Assert.That(input.Current.Left.Speed,Is.Zero);
  }
  [Test] public void CameraOnlySliceKeepsOtherHandActiveAndRebasesRecoveredHand(){
   var input=new JungleGameplayInput();input.Select(JungleGame.CoconutSlice,true);
   var first=Camera(time:1000,seq:1);input.UpdateCamera(first,PlayerLockState.LOCKED,1,1001);
   var partial=Camera(time:1100,seq:2);partial.subjects[0].confidence=.1f;
   Array.Find(partial.subjects[0].landmarks,p=>p.name=="right-wrist").visible=false;
   Array.Find(partial.subjects[0].landmarks,p=>p.name=="left-wrist").x+=.1f;
   input.UpdateCamera(partial,PlayerLockState.LOCKED,1,1101);
   Assert.That(input.Ready,Is.True);Assert.That(input.Current.Pose.rightDetected,Is.False);
   Assert.That(input.Current.Actions&JungleAction.SliceLeft,Is.EqualTo(JungleAction.SliceLeft));
   Assert.That(input.Current.Actions&JungleAction.SliceRight,Is.EqualTo(JungleAction.None));
   var recovered=Camera(time:1200,seq:3);Array.Find(recovered.subjects[0].landmarks,p=>p.name=="right-wrist").x=.9f;
   input.UpdateCamera(recovered,PlayerLockState.LOCKED,1,1201);
   Assert.That(input.Ready,Is.True);Assert.That(input.Current.Right.Speed,Is.Zero);
   Assert.That(input.Current.Actions&JungleAction.SliceRight,Is.EqualTo(JungleAction.None));
   var lost=Camera(time:1300,seq:4);Array.Find(lost.subjects[0].landmarks,p=>p.name=="left-wrist").visible=false;Array.Find(lost.subjects[0].landmarks,p=>p.name=="right-wrist").visible=false;
   input.UpdateCamera(lost,PlayerLockState.LOCKED,1,1301);Assert.That(input.Ready,Is.False);
  }
  [Test] public void CameraOnlyAssociationAcceptsOneHandButRejectsMultiplePeople(){
   var l=new PlayerLock(new FusionSettings());l.Ready(1);
   var a=Camera(seq:1);var b=Camera(seq:2);
   foreach(var f in new[]{a,b}){f.subjects[0].confidence=.1f;Array.Find(f.subjects[0].landmarks,p=>p.name=="right-wrist").visible=false;l.Observe(f,true);}
   Assert.That(l.State,Is.EqualTo(PlayerLockState.LOCKED));
   l.Observe(Camera("second-person-enters",seq:3),true);Assert.That(l.State,Is.EqualTo(PlayerLockState.LOST));
  }
  [Test] public void CameraOnlySliceUsesFreshObservedHandsAndRejectsOtherGames(){
   var input=new JungleGameplayInput();input.Select(JungleGame.CoconutSlice,true);
   var first=Camera(time:1000,seq:1);
   input.UpdateCamera(first,PlayerLockState.LOCKED,1,1001);
   Assert.That(input.Ready,Is.True);Assert.That(input.Current.Baseline,Is.True);
   Assert.That(input.Authorize(JungleAction.SliceLeft,input.Current.Revision,1001),Is.False);
   var next=Camera(time:1100,seq:2);Array.Find(next.subjects[0].landmarks,p=>p.name=="left-wrist").x+=.1f;
   input.UpdateCamera(next,PlayerLockState.LOCKED,1,1101);
   Assert.That(input.Current.Actions&JungleAction.SliceLeft,Is.EqualTo(JungleAction.SliceLeft));
   Assert.That(input.Authorize(JungleAction.SliceLeft,input.Current.Revision,1101),Is.True);
   Assert.That(input.Authorize(JungleAction.SliceLeft,input.Current.Revision,1101),Is.False);
   input.UpdateCamera(next,PlayerLockState.LOST,1,1102);Assert.That(input.Ready,Is.False);
   input.Select(JungleGame.Bowling,true);input.UpdateCamera(next,PlayerLockState.LOCKED,1,1102);Assert.That(input.Ready,Is.False);
  }
  [Test] public void BowlingHookHasDeadZoneAndIndependentBoundedDirections(){
   Assert.That(ShotMechanics.BowlingSpin(0),Is.Zero);
   Assert.That(ShotMechanics.BowlingSpin(4),Is.Zero);
   Assert.That(ShotMechanics.BowlingSpin(25),Is.EqualTo(.5f).Within(.001));
   Assert.That(ShotMechanics.BowlingSpin(-25),Is.EqualTo(-.5f).Within(.001));
   Assert.That(ShotMechanics.BowlingSpin(90),Is.EqualTo(1));
   Assert.That(ShotMechanics.BowlingCurveAcceleration(-1),Is.LessThan(0));
   Assert.That(ShotMechanics.BowlingCurveAcceleration(1),Is.GreaterThan(0));
  }
  [Test] public void SlicePrecisionRewardsCenterAndHasBoundedMultiplier(){
   Assert.That(ShotMechanics.SlicePrecisionMultiplier(0,.075f),Is.EqualTo(1.5f));
   Assert.That(ShotMechanics.SlicePrecisionMultiplier(.0375f,.075f),Is.EqualTo(1.25f).Within(.001));
   Assert.That(ShotMechanics.SlicePrecisionMultiplier(.075f,.075f),Is.EqualTo(1));
   Assert.That(ShotMechanics.SlicePrecisionMultiplier(.2f,.075f),Is.EqualTo(1));
  }
  [Test] public void CameraFramingAndHandDwellAndGoalieSaveWorkDeterministically(){
   var f=Camera();foreach(var p in f.subjects[0].landmarks){p.visible=true;p.visibility=p.presence=1;}
   var framing=ShotMechanics.EvaluateFraming(true,f,true);
   Assert.That(framing.Status,Is.EqualTo(CameraFramingStatus.Optimal));
   var nav=new HandDwellNavigator();var targets=new[]{new HandDwellTarget(3,10,10,100,100)};
   Assert.That(nav.ObserveCursor(true,50,50,0f,targets,out _,0.5f),Is.False);
   Assert.That(nav.ObserveCursor(true,50,50,0.25f,targets,out _,0.5f),Is.False);
   Assert.That(nav.ObserveCursor(true,50,50,0.50f,targets,out int hit,0.5f),Is.True);
   Assert.That(hit,Is.EqualTo(3));
   var save=ShotMechanics.EvaluateGoalieSave(.5f,.45f,false,0,0,0,false,0,0,0,true,.5f,.45f);
   Assert.That(save.Saved,Is.True);Assert.That(save.Source,Is.EqualTo(GoalieSaveSource.Torso));
  }
  [Test] public void VisualHandPredictionIsBoundedAndExpires(){
   var visual=new HandVisualPrediction();
   Assert.That(visual.TryPosition(1,out _,out _),Is.False);
   visual.Observe(.5f,.5f,4f,0f,1f);
   Assert.That(visual.TryPosition(1.04f,out var x,out var y),Is.True);
   Assert.That(x,Is.EqualTo(.5f).Within(.0001f));Assert.That(y,Is.EqualTo(.5f));
   Assert.That(visual.TryPosition(1.3f,out _,out _),Is.False);
   visual.Reset();Assert.That(visual.TryPosition(1.04f,out _,out _),Is.False);
  }
  [Test] public void TrackingPauseDoesNotFlickerDuringIntermittentRecovery(){
   var gate=new TrackingPauseStability();
   Assert.That(gate.Update(false,0),Is.True);
   Assert.That(gate.ShowNotice(.1f),Is.False);
   Assert.That(gate.Update(true,.1f),Is.True);
   Assert.That(gate.Update(false,.2f),Is.True);
   Assert.That(gate.ShowNotice(.31f),Is.True);
   Assert.That(gate.Update(true,.35f),Is.True);
   Assert.That(gate.Update(true,.84f),Is.True);
   Assert.That(gate.Update(true,.86f),Is.False);
   Assert.That(gate.ShowNotice(.86f),Is.False);
   Assert.That(gate.Update(false,1f),Is.True);
   Assert.That(gate.ShowNotice(1.1f),Is.False);
   Assert.That(gate.ShowNotice(1.31f),Is.True);
   gate.Reset();Assert.That(gate.Update(true,2f),Is.True);
  }
  [Serializable]public sealed class Corpus{public Case[]cameraCases;public string[]fusionCases,conceptSequences;public Scenario[]fusionScenarios;public Trace[]conceptualTraces;}
  [Serializable]public sealed class Scenario{public string name,connectionState,cameraCase,operation;public long phoneTime,cameraTime,now;public int phonePlayer,activePlayer;public bool expectedEligible;}
  [Serializable]public sealed class Trace{public string name;public TraceSample[]samples;}
  [Serializable]public sealed class TraceSample{public int relativeTimestamp;public float accelerationX,rotationAlpha;public string cameraCase;}
  [Serializable]public sealed class Case{public string name;public CameraInputWire frame;}
  static Corpus Read()=>JsonUtility.FromJson<Corpus>(File.ReadAllText(Path.GetFullPath(Path.Combine(Application.dataPath,"../../Shared/Protocol/camera/corpus-v1.json"))));
  static CameraInputWire Camera(string name="person-neutral",long time=1000,long seq=1){var f=Read().cameraCases.First(x=>x.name==name).frame;f.captureTimestamp=time;f.processTimestamp=time+1;f.frameSequence=seq;return f;}
  static PhoneInputWire Phone(long time=1000,long seq=1,int player=1)=>new PhoneInputWire{playerId=player,sequence=seq,connectionEpoch=1,sessionId="lab",deviceSessionId="device"+player,serverReceiveTimestamp=time,clientTimestamp=time-3,messageType="MOTION_SAMPLE",connectionState="ACTIVE",quality=new PhoneQuality{source="synthetic",status="valid"},capabilities=new[]{"acceleration"},acceleration=new PhoneValues{availability="partial",hasX=true,x=player}};
  static InputFusion Ready(PhoneInputStore store,FusionSettings settings=null){var f=new InputFusion(store,settings??new FusionSettings());f.Association.Ready(1);f.ObserveCamera(Camera(seq:1),1001);f.ObserveCamera(Camera(seq:2),1001);return f;}
  static void Add(InputFusion fusion,PhoneInputStore store,PhoneInputWire p){Assert.That(store.Accept(p),Is.True);fusion.ObservePhone(p);}
  [Test] public void CameraGuidanceExplainsVisibleProblemsWithoutAssumingLighting(){
   Assert.That(CameraGuidance.Describe(false,null,true),Does.Contain("no está enviando"));
   Assert.That(CameraGuidance.Describe(true,Camera("tracking-lost"),true),Does.Contain("Ponte frente"));
   Assert.That(CameraGuidance.Describe(true,Camera("second-person-enters"),true),Does.Contain("varias personas"));
   var f=Camera();foreach(var p in f.subjects[0].landmarks){p.visible=true;p.visibility=p.presence=1;}
   var left=Array.Find(f.subjects[0].landmarks,p=>p.name=="left-wrist");left.visible=false;
   Assert.That(CameraGuidance.Describe(true,f,true),Does.Contain("izquierda"));
   left.visible=true;Assert.That(CameraGuidance.Describe(true,f,true),Does.Contain("detecta"));
  }
  [Test] public void CameraVisualPredictionIsBoundedAndDoesNotInventInvisibleLandmarks(){
   var store=new CameraInputStore();var first=Camera(time:1000,seq:1);var second=Camera(time:1050,seq:2);
   var a=Array.Find(first.subjects[0].landmarks,p=>p.name=="right-wrist");
   var b=Array.Find(second.subjects[0].landmarks,p=>p.name=="right-wrist");
   a.visible=b.visible=true;a.x=.4f;a.y=.5f;b.x=.5f;b.y=.5f;
   Assert.That(store.Accept(first,1001),Is.True);Assert.That(store.Accept(second,1051),Is.True);
   Assert.That(store.TryVisualLandmark(second.subjects[0].subjectId,"right-wrist",1100,out var x,out var y),Is.True);
   Assert.That(x,Is.EqualTo(.55f).Within(.0001));Assert.That(y,Is.EqualTo(.5f).Within(.0001));
   Assert.That(b.x,Is.EqualTo(.5f),"observations must not be modified");
   Assert.That(store.TryVisualLandmark(second.subjects[0].subjectId,"right-wrist",1171,out _,out _),Is.False);
   b.visible=false;Assert.That(store.TryVisualLandmark(second.subjects[0].subjectId,"right-wrist",1100,out _,out _),Is.False);
  }
  [Test] public void PhoneVisualEstimateIsBoundedFreshAndIsolatedBySession(){
   var store=new PhoneInputStore();var fusion=new InputFusion(store,new FusionSettings());
   var first=Phone(1000,1);first.orientation=new PhoneValues{hasAlpha=true,alpha=359};first.rotationRate=new PhoneValues{hasBeta=true,beta=10};
   var second=Phone(1050,2);second.orientation=new PhoneValues{hasAlpha=true,alpha=1};second.rotationRate=new PhoneValues{hasBeta=true,beta=30};
   second.accelerationIncludingGravity=new PhoneValues{hasX=true,x=9};
   Add(fusion,store,first);Add(fusion,store,second);
   Assert.That(fusion.TryPhoneVisualEstimate(1,1100,out var visual),Is.True);
   Assert.That(visual.Orientation.alpha,Is.EqualTo(3).Within(.001));
   Assert.That(visual.RotationRate.beta,Is.EqualTo(50).Within(.001));
   Assert.That(visual.AccelerationIncludingGravity.x,Is.EqualTo(9));
   Assert.That(visual.AccelerationIncludingGravity.hasY,Is.False);
   Assert.That(fusion.TryPhoneVisualEstimate(1,1171,out _),Is.False);
   var reconnect=Phone(1200,1);reconnect.connectionEpoch=2;reconnect.orientation=new PhoneValues{hasAlpha=true,alpha=50};
   Add(fusion,store,reconnect);Assert.That(fusion.TryPhoneVisualEstimate(1,1250,out visual),Is.True);
   Assert.That(visual.Orientation.alpha,Is.EqualTo(50));
   var disconnect=Phone(1260,1);disconnect.connectionEpoch=2;disconnect.messageType="SERVER_STATE";disconnect.connectionState="DISCONNECTED";
   store.Accept(disconnect);Assert.That(fusion.TryPhoneVisualEstimate(1,1260,out _),Is.False);
  }
  [TestCase("phone-camera-aligned")][TestCase("phone-early")][TestCase("phone-late")][TestCase("camera-early")][TestCase("camera-late")][TestCase("phone-stale")][TestCase("camera-stale")][TestCase("both-stale")][TestCase("camera-lost")][TestCase("camera-recovered")][TestCase("phone-lost")][TestCase("phone-recovered")][TestCase("low-confidence")][TestCase("partial-capabilities")][TestCase("duplicate-phone")][TestCase("reordered-phone")][TestCase("duplicate-camera")][TestCase("player-mismatch")][TestCase("wrong-player")][TestCase("second-subject")][TestCase("recovery-after-loss")]
  public void SharedFusionScenarios(string name){var c=Read().fusionScenarios.First(x=>x.name==name);var store=new PhoneInputStore();var f=new InputFusion(store,new FusionSettings());f.Association.Ready(c.activePlayer);f.ObserveCamera(Camera(c.cameraCase,c.cameraTime,1),c.cameraTime+1);f.ObserveCamera(Camera(c.cameraCase,c.cameraTime,2),c.cameraTime+1);var p=Phone(c.phoneTime,2,c.phonePlayer);p.connectionState=c.connectionState;Add(f,store,p);
   if(c.operation=="duplicate-phone"){Assert.That(store.Accept(Phone(c.phoneTime,2)),Is.False);f.ObservePhone(Phone(c.phoneTime,2));}
   if(c.operation=="reordered-phone"){Assert.That(store.Accept(Phone(c.phoneTime,1)),Is.False);f.ObservePhone(Phone(c.phoneTime,1));}
   if(c.operation=="duplicate-camera")Assert.That(f.ObserveCamera(Camera(c.cameraCase,c.cameraTime,2),c.cameraTime+1),Is.False);
   if(c.operation=="camera-loss-recover"){f.ObserveCamera(Camera("tracking-lost",c.cameraTime,3),c.cameraTime+1);Assert.That(f.Evaluate(1,c.now).Eligible,Is.False);f.ObserveCamera(Camera(time:c.cameraTime,seq:4),c.cameraTime+1);Assert.That(f.Association.State,Is.EqualTo(PlayerLockState.RECOVERING));f.ObserveCamera(Camera(time:c.cameraTime,seq:5),c.cameraTime+1);}
   if(c.operation=="phone-loss-recover"){var d=Phone(c.phoneTime,2);d.messageType="SERVER_STATE";d.connectionState="DISCONNECTED";store.Accept(d);Assert.That(f.Evaluate(1,c.now).Eligible,Is.False);var r=Phone(c.phoneTime,1);r.connectionEpoch=2;Add(f,store,r);}
   Assert.That(f.Evaluate(1,c.now).Eligible,Is.EqualTo(c.expectedEligible),name);
  }
  [Test]public void ConceptualTracesRetainEverySampleWithoutInferringIntention(){foreach(var trace in Read().conceptualTraces){var store=new PhoneInputStore();var f=Ready(store);int seq=0;foreach(var sample in trace.samples){long time=1000+sample.relativeTimestamp;var p=Phone(time,++seq);p.acceleration.x=sample.accelerationX;p.rotationRate=new PhoneValues{availability="partial",hasAlpha=true,alpha=sample.rotationAlpha};Add(f,store,p);f.ObserveCamera(Camera(sample.cameraCase,time,seq+2),time+1);var r=f.Evaluate(1,time+1);Assert.That(r.Phone.acceleration.x,Is.EqualTo(sample.accelerationX),trace.name);Assert.That(r.Phone.rotationRate.alpha,Is.EqualTo(sample.rotationAlpha));Assert.That(r.Camera.subjects[0].landmarks[0].name,Is.EqualTo("left-shoulder"));}}}
  [Test]public void RejectedFusionRetainsRawQualityAgeAndTrackingWithoutInventingUsableSignals(){var store=new PhoneInputStore();var f=Ready(store);Add(f,store,Phone());f.ObserveCamera(Camera("tracking-low-confidence",1000,3),1001);var r=f.Evaluate(1,1100);Assert.That(r.Eligible,Is.False);Assert.That(r.CameraFresh,Is.False);Assert.That(r.CameraFrameFresh,Is.True);Assert.That(r.RawCamera.subjects[0].confidence,Is.EqualTo(.2f));Assert.That(r.RawCamera.source,Is.EqualTo("synthetic"));Assert.That(r.RawCameraAge,Is.EqualTo(100));Assert.That(r.RawPhoneAge,Is.EqualTo(100));r=f.Evaluate(1,1400);Assert.That(r.Eligible,Is.False);Assert.That(r.PhoneReceiptFresh,Is.False);Assert.That(r.RawCameraAge,Is.EqualTo(400));Assert.That(r.RawPhone.quality.source,Is.EqualTo("synthetic"));}
  [TestCase("physical-webcam",false)][TestCase("synthetic",true)][TestCase("replay",true)]
  public void RejectedRawCameraSourceControlsFixtureLogging(string source,bool allowed){
   var fusion=new InputFusion(new PhoneInputStore(),new FusionSettings());var camera=Camera();camera.source=source;
   Assert.That(fusion.ObserveCamera(camera,1001),Is.True);var rejected=fusion.Evaluate(1,1001);
   Assert.That(rejected.Eligible,Is.False);Assert.That(rejected.Camera,Is.Null);
   Assert.That(CameraFusionLabRunner.CanLogFixtureObservation(rejected),Is.EqualTo(allowed));
  }
  [Test]public void PhysicalPhoneCannotEscapeThroughSyntheticCameraDiagnostic(){
   var store=new PhoneInputStore();var fusion=Ready(store);var phone=Phone();phone.quality.source="physical";Add(fusion,store,phone);
   Assert.That(CameraFusionLabRunner.CanLogFixtureObservation(fusion.Evaluate(1,1001)),Is.False);
  }
  [Test]public void AbsentRawInputNeverGeneratesFixtureObservation(){
   var fusion=new InputFusion(new PhoneInputStore(),new FusionSettings());
   Assert.That(CameraFusionLabRunner.CanLogFixtureObservation(fusion.Evaluate(1,1001)),Is.False);
  }
  [Test]public void CorpusHasAllRequiredCameraAndConceptualCases(){var c=Read();Assert.That(c.cameraCases.Length,Is.EqualTo(12));Assert.That(c.fusionCases.Length,Is.EqualTo(21));Assert.That(c.conceptSequences.Length,Is.EqualTo(8));foreach(var x in c.cameraCases)Assert.That(CameraInputStore.Valid(x.frame),Is.True,x.name);}
  [Test]public void ContractRejectsNonFiniteAndMissingLandmarks(){var f=Camera();f.subjects[0].landmarks[0].x=float.NaN;Assert.That(CameraInputStore.Valid(f),Is.False);f=Camera();f.subjects[0].landmarks=new CameraLandmark[0];Assert.That(CameraInputStore.Valid(f),Is.False);f=Camera();f.subjects[0].confidence=2;Assert.That(CameraInputStore.Valid(f),Is.False);}
  [Test]public void CameraBufferRejectsDuplicatesReorderInvalidAndBoundsHistory(){var s=new CameraInputStore();for(int i=1;i<=50;i++)Assert.That(s.Accept(Camera(seq:i),1001),Is.True);Assert.That(s.Frames.Count,Is.EqualTo(16));Assert.That(s.Accept(Camera(seq:50),1001),Is.False);Assert.That(s.Accept(Camera(seq:49),1001),Is.False);Assert.That(s.Accept(Camera(time:2000,seq:51),1001),Is.False);Assert.That(s.Age(1050),Is.EqualTo(50));Assert.That(s.Fresh(1050,100,20),Is.True);}
  [Test]public void PlayerLockLossRecoveryNeverSelectsAnotherSubject(){var l=new PlayerLock(new FusionSettings());Assert.That(l.State,Is.EqualTo(PlayerLockState.UNASSIGNED));l.Ready(1);l.Observe(Camera(seq:1));Assert.That(l.State,Is.EqualTo(PlayerLockState.ACQUIRING));l.Observe(Camera(seq:2));Assert.That(l.State,Is.EqualTo(PlayerLockState.LOCKED));l.Observe(Camera("tracking-lost",seq:3));Assert.That(l.State,Is.EqualTo(PlayerLockState.LOST));var wrong=Camera(seq:4);wrong.subjects[0].subjectId=2;l.Observe(wrong);Assert.That(l.SubjectId,Is.EqualTo(1));Assert.That(l.State,Is.EqualTo(PlayerLockState.LOST));l.Observe(Camera(seq:5));Assert.That(l.State,Is.EqualTo(PlayerLockState.RECOVERING));l.Observe(Camera(seq:6));Assert.That(l.State,Is.EqualTo(PlayerLockState.LOCKED));}
  [TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)][TestCase(7)][TestCase(8)][TestCase(9)][TestCase(10)][TestCase(11)][TestCase(12)][TestCase(13)][TestCase(14)][TestCase(15)][TestCase(16)][TestCase(17)][TestCase(18)][TestCase(19)][TestCase(20)]
  public void TwentyDeterministicReplacementsCannotPromoteForeignSubject(int scenario){
   int player=1+(scenario-1)%4;var store=new PhoneInputStore();var fusion=new InputFusion(store,new FusionSettings());
   fusion.Association.Ready(player);fusion.ObserveCamera(Camera(seq:1),1001);fusion.ObserveCamera(Camera(seq:2),1001);
   var phone=Phone(player:player);Add(fusion,store,phone);
   Assert.That(fusion.Evaluate(player,1001).Eligible,Is.True);
   for(int sequence=3;sequence<=4;sequence++){
    var foreign=Camera(seq:sequence);foreign.subjects[0].subjectId=100+scenario;
    foreign.subjects[0].centerX=.3f+scenario*.01f;
    Assert.That(fusion.ObserveCamera(foreign,1001),Is.True);
    Assert.That(fusion.Association.SubjectId,Is.EqualTo(1),"unauthorized switch, scenario "+scenario);
    Assert.That(fusion.Association.PlayerId,Is.EqualTo(player));
    Assert.That(fusion.Association.State,Is.EqualTo(PlayerLockState.LOST));
    Assert.That(fusion.Evaluate(player,1001).Eligible,Is.False,"safe gate during replacement");
   }
   fusion.ObserveCamera(Camera("tracking-lost",seq:5),1001);
   Assert.That(fusion.Evaluate(player,1001).Eligible,Is.False);
   fusion.ObserveCamera(Camera(seq:6),1001);
   Assert.That(fusion.Association.State,Is.EqualTo(PlayerLockState.RECOVERING));
   Assert.That(fusion.Evaluate(player,1001).Eligible,Is.False,"one recovery frame cannot resume");
   fusion.ObserveCamera(Camera(seq:7),1001);
   Assert.That(fusion.Association.State,Is.EqualTo(PlayerLockState.LOCKED));
   var result=fusion.Evaluate(player,1001);Assert.That(result.Eligible,Is.True);
   Assert.That(result.SubjectId,Is.EqualTo(1));Assert.That(result.Phone.playerId,Is.EqualTo(player));
   Assert.That(result.Phone.sessionId,Is.EqualTo("lab"));Assert.That(result.Phone.deviceSessionId,Is.EqualTo("device"+player));
   Assert.That(result.Phone.connectionEpoch,Is.EqualTo(1));
  }
  [Test]public void SecondPersonRoiCrossingLowConfidenceAndAmbiguityLoseLock(){foreach(var name in new[]{"second-person-enters","tracking-low-confidence","hand-occluded"}){var l=new PlayerLock(new FusionSettings());l.Ready(1);l.Observe(Camera(seq:1));l.Observe(Camera(seq:2));l.Observe(Camera(name,seq:3));Assert.That(l.State,Is.EqualTo(name=="hand-occluded"?PlayerLockState.LOCKED:PlayerLockState.LOST),name);}var p=new PlayerLock(new FusionSettings());p.Ready(1);p.Observe(Camera(seq:1));p.Observe(Camera(seq:2));var f=Camera(seq:3);f.subjects[0].ambiguous=true;p.Observe(f);Assert.That(p.State,Is.EqualTo(PlayerLockState.LOST));}
  [Test]public void SpatialJumpAndStaleFramesDoNotRefreshLock(){var l=new PlayerLock(new FusionSettings());l.Ready(1);l.Observe(Camera(seq:1));l.Observe(Camera(seq:2));var f=Camera(seq:3);f.subjects[0].centerX=.75f;l.Observe(f);Assert.That(l.State,Is.EqualTo(PlayerLockState.LOST));l.Observe(Camera(seq:2));Assert.That(l.State,Is.EqualTo(PlayerLockState.LOST));}
  [TestCase(0,true)] [TestCase(-80,true)] [TestCase(80,true)] [TestCase(-150,false)] [TestCase(150,false)]
  public void EarlyLateBothDirectionsPreserveDelta(long delta,bool aligned){var s=new PhoneInputStore();var f=Ready(s,new FusionSettings{maxPhoneAge=400});Add(f,s,Phone(1000+delta));var r=f.Evaluate(1,1150);Assert.That(r.Aligned,Is.EqualTo(aligned));Assert.That(r.AlignmentDelta,Is.EqualTo(Math.Abs(delta)));Assert.That(r.SubjectId,Is.EqualTo(1));}
  [Test]public void StaleLostRecoveredAndFutureSamplesAreExplicit(){var s=new PhoneInputStore();var f=Ready(s);Add(f,s,Phone());Assert.That(f.Evaluate(1,1100).Eligible,Is.True);Assert.That(f.Evaluate(1,1400).Eligible,Is.False);Assert.That(f.Evaluate(1,1400).PhoneFresh,Is.False);Assert.That(f.Evaluate(1,1400).CameraFresh,Is.False);f.ObserveCamera(Camera(time:1500,seq:3),1501);f.ObserveCamera(Camera(time:1500,seq:4),1501);Add(f,s,Phone(1500,2));Assert.That(f.Evaluate(1,1501).Eligible,Is.True);Add(f,s,Phone(9000,3));Assert.That(f.Evaluate(1,1502).Phone.sequence,Is.EqualTo(2));}
  [Test]public void CameraFreshPhoneStaleAndPhoneFreshCameraStale(){var s=new PhoneInputStore();var f=Ready(s);Add(f,s,Phone(500));Assert.That(f.Evaluate(1,1100).Status,Is.EqualTo(AlignmentStatus.PHONE_DEGRADED));Add(f,s,Phone(1500,2));var r=f.Evaluate(1,1501);Assert.That(r.PhoneFresh,Is.True);Assert.That(r.CameraFresh,Is.False);Assert.That(r.Eligible,Is.False);}
  [Test]public void PhoneDisconnectReconnectEpochAndPartialSignals(){var s=new PhoneInputStore();var f=Ready(s);Add(f,s,Phone());var r=f.Evaluate(1,1005);Assert.That(r.AvailableSignals,Does.Contain("acceleration"));Assert.That(r.AvailableSignals,Does.Not.Contain("rotationRate"));Assert.That(r.Phone.acceleration.hasY,Is.False);var state=Phone(seq:1);state.messageType="SERVER_STATE";state.connectionState="DISCONNECTED";s.Accept(state);Assert.That(f.Evaluate(1,1005).Eligible,Is.False);var resume=Phone(1010,1);resume.connectionEpoch=2;Add(f,s,resume);Assert.That(f.Evaluate(1,1011).Eligible,Is.True);}
  [Test]public void PlayerIsolationAndInvalidPlayerNeverBecomeEligible(){var s=new PhoneInputStore();var f=Ready(s);for(int p=1;p<=4;p++)Add(f,s,Phone(player:p));for(int p=1;p<=4;p++){var r=f.Evaluate(p,1010);Assert.That(r.Phone.playerId,Is.EqualTo(p));Assert.That(r.Eligible,Is.EqualTo(p==1));}Assert.That(f.Evaluate(0,1010).Eligible,Is.False);Assert.Throws<ArgumentOutOfRangeException>(()=>f.Association.Ready(5));}
  [Test]public void DifferentCadencesSelectNearestHistoricalPairWithoutOneToOne(){var s=new PhoneInputStore();var f=Ready(s);Add(f,s,Phone(990,1));Add(f,s,Phone(1020,2));Add(f,s,Phone(1050,3));var r=f.Evaluate(1,1051);Assert.That(r.Phone.sequence,Is.EqualTo(1));Assert.That(r.AlignmentDelta,Is.EqualTo(10));}
  [Test]public void PhoneBufferRejectsDuplicateReorderedAndBoundsReplay(){var s=new PhoneInputStore();var f=Ready(s);for(int i=1;i<=100;i++)Add(f,s,Phone(900+i,i));f.ObservePhone(Phone(1000,100));f.ObservePhone(Phone(999,99));Assert.That(f.DuplicateOrReordered,Is.EqualTo(2));Assert.That(f.Evaluate(1,1001).Phone.sequence,Is.EqualTo(100));}
  [Test]public void ConceptSequencesRouteSignalsWithoutSportsSemantics(){var s=new PhoneInputStore();var f=Ready(s);int i=0;foreach(var name in Read().conceptSequences){var p=Phone(1000,++i);p.acceleration.x=i;p.rotationRate=new PhoneValues{availability="partial",hasAlpha=true,alpha=i*2};Add(f,s,p);f.ObserveCamera(Camera(time:1000,seq:i+2),1001);var r=f.Evaluate(1,1001);Assert.That(r.Phone.rotationRate.alpha,Is.EqualTo(i*2),name);Assert.That(r.Phone.acceleration.x,Is.EqualTo(i),name);Assert.That(r.Phone.rotationRate.hasBeta,Is.False);}}
 }
}
