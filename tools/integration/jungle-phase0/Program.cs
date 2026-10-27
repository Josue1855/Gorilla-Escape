using System;
using System.Collections.Generic;
using GorillaEscape.Contracts;
using GorillaEscape.Input;
using GorillaEscape.Input.Fusion;
class Checks
{
    static PhoneValues Angles() => new PhoneValues { availability="present", hasAlpha=true, hasBeta=true, hasGamma=true, alpha=10, beta=20, gamma=30 };
    static PhoneInputWire Phone(long sequence=1) => new PhoneInputWire {
        playerId=1, messageType="MOTION_SAMPLE", sessionId="session", deviceSessionId="device", connectionEpoch=1,
        sequence=sequence, serverReceiveTimestamp=1000, connectionState="ACTIVE",
        accelerationIncludingGravity=new PhoneValues { availability="present", hasX=true,hasY=true,hasZ=true,x=1,y=2,z=9.81f },
        rotationRate=Angles(), orientation=Angles(), touch=new PhoneValues {hasPressed=true,pressed=false}
    };
    static CameraInputWire Camera(long seq=1) {
        var names=new[]{"left-shoulder","right-shoulder","left-elbow","right-elbow","left-wrist","right-wrist"};
        var landmarks=new List<CameraLandmark>();
        foreach(var name in names) landmarks.Add(new CameraLandmark { name=name,x=.2f,y=.3f,visibility=.9f,presence=.9f,visible=true });
        return new CameraInputWire {version=1,width=640,height=480,frameSequence=seq,captureTimestamp=1000,processTimestamp=1001,
            clockDomain="pc-unix-ms",captureTimeSource="fixture",source="synthetic",quality="fixture",trackingState="TRACKING",
            subjects=new[]{new CameraSubject {subjectId=1,centerX=.5f,centerY=.5f,confidence=.9f,landmarks=landmarks.ToArray()}}};
    }
    static void Require(bool condition) { if(!condition) throw new Exception("assertion failed"); }
    static int Main() {
        int failures=0;
        var tests=new (string,Action)[]{
            ("preserves real sensor units and axes",()=>{var a=new JungleInputAdapter();Require(a.TryMotion(Phone(),1000,out var p)&&p.ax==1&&p.az==9.81f&&p.rx==10&&p.beta==20);}),
            ("rejects duplicate and reordered samples",()=>{var a=new JungleInputAdapter();a.TryMotion(Phone(2),1000,out _);Require(!a.TryMotion(Phone(2),1000,out _)&&!a.TryMotion(Phone(1),1000,out _));}),
            ("rejects stale and future timestamps",()=>{var a=new JungleInputAdapter();Require(!a.TryMotion(Phone(),1300,out _)&&!a.TryMotion(Phone(),900,out _));}),
            ("rejects disconnected and other player input",()=>{var a=new JungleInputAdapter();var p=Phone();p.connectionState="DISCONNECTED";Require(!a.TryMotion(p,1000,out _));p=Phone();p.playerId=2;Require(!a.TryMotion(p,1000,out _));}),
            ("does not invent unavailable axes",()=>{var a=new JungleInputAdapter();var p=Phone();p.accelerationIncludingGravity.hasY=false;Require(!a.TryMotion(p,1000,out _));}),
            ("rejects nonfinite values",()=>{var p=Phone();p.orientation.beta=float.NaN;Require(!new JungleInputAdapter().TryMotion(p,1000,out _));}),
            ("new epoch accepts reset sequence",()=>{var a=new JungleInputAdapter();a.TryMotion(Phone(10),1000,out _);var p=Phone();p.connectionEpoch=2;Require(a.TryMotion(p,1000,out _));}),
            ("touch confirms once per observed rising edge",()=>{var a=new JungleInputAdapter();var p=Phone();a.TryMotion(p,1000,out _);p=Phone(2);p.touch.pressed=true;Require(a.TryMotion(p,1000,out var q)&&q.confirm==1);p.sequence=3;Require(a.TryMotion(p,1000,out q)&&q.confirm==1);}),
            ("recenter uses its own virtual touch zone",()=>{var a=new JungleInputAdapter();var p=Phone();p.touch.hasX=true;p.touch.x=1;p.touch.pressed=true;Require(a.TryMotion(p,1000,out var q)&&q.recenter==1&&q.confirm==0);}),
            ("preserves mirror and hand identity",()=>{var a=new JungleInputAdapter();Require(a.TryPose(Camera(),1,1002,out var p)&&p.leftDetected&&p.rightDetected&&p.rightWristX==.8f&&p.rightWristY==.3f);}),
            ("rejects unassigned ambiguous and stale poses",()=>{var a=new JungleInputAdapter();Require(!a.TryPose(Camera(),2,1002,out _)&&!a.TryPose(Camera(),1,1300,out _));var f=Camera();f.subjects[0].ambiguous=true;Require(!a.TryPose(f,1,1002,out _));}),
            ("missing wrist stays explicitly undetected",()=>{var f=Camera();f.subjects[0].landmarks[5].visible=false;Require(new JungleInputAdapter().TryPose(f,1,1002,out var p)&&p.leftDetected&&!p.rightDetected&&!p.detected);}),
            ("pose duplicate never replays",()=>{var a=new JungleInputAdapter();a.TryPose(Camera(2),1,1002,out _);Require(!a.TryPose(Camera(2),1,1002,out _));}),
            ("player lock acquires from two real frames",()=>{var l=new PlayerLock(new FusionSettings());l.Ready(1);l.Observe(Camera());Require(l.State==PlayerLockState.ACQUIRING);l.Observe(Camera(2));Require(l.State==PlayerLockState.LOCKED);}),
            ("second camera candidate invalidates association",()=>{var l=new PlayerLock(new FusionSettings());l.Ready(1);l.Observe(Camera());l.Observe(Camera(2));var f=Camera(3);var s=f.subjects[0];f.subjects=new[]{s,new CameraSubject{subjectId=2,centerX=.5f,centerY=.5f,confidence=.9f,landmarks=s.landmarks}};l.Observe(f);Require(l.State==PlayerLockState.LOST);})
        };
        foreach(var test in tests) {try{test.Item2();Console.WriteLine("PASS "+test.Item1);}catch(Exception e){failures++;Console.WriteLine("FAIL "+test.Item1+": "+e.Message);}}
        Console.WriteLine($"{tests.Length-failures}/{tests.Length} passed (pure C# contracts; not Unity or physical acceptance)");
        return failures==0?0:1;
    }
}
