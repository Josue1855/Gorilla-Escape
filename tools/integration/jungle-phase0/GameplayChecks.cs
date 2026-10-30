using System;
using System.Collections.Generic;
using GorillaEscape.Contracts;
using GorillaEscape.Input;
using GorillaEscape.Input.Fusion;
using GorillaEscape.Gameplay.Input;

static class GameplayChecks
{
    static void Require(bool pass) { if(!pass)throw new Exception("assertion failed"); }
    internal sealed class Lab
    {
        public readonly PhoneInputStore Phones=new PhoneInputStore();
        public readonly InputFusion Fusion;
        public readonly JungleGameplayInput Game=new JungleGameplayInput();
        public long At,Sequence,CameraSequence;
        public Lab(JungleGame game) {Fusion=new InputFusion(Phones,new FusionSettings());Fusion.Association.Ready(1);Game.Select(game);}
        public void Step(long at,float angular=0,float rightY=.3f,float leftY=.3f,bool right=true,bool left=true,long epoch=1,bool partial=false,float gamma=30,float screen=0,int subject=1)
        {
            At=at+2;
            var p=Checks.Phone(++Sequence);p.serverReceiveTimestamp=at;p.connectionEpoch=epoch;
            p.orientation.gamma=gamma;p.screenOrientation.angle=screen;
            p.rotationRate.alpha=angular;p.rotationRate.beta=p.rotationRate.gamma=0;
            p.accelerationIncludingGravity.x=p.accelerationIncludingGravity.y=0;p.accelerationIncludingGravity.z=9.81f;
            if(partial)p.orientation.hasBeta=false;
            Require(Phones.Accept(p));Fusion.ObservePhone(p);
            var c=Checks.Camera(++CameraSequence);c.captureTimestamp=at;c.processTimestamp=at+1;
            c.subjects[0].subjectId=subject;
            c.subjects[0].landmarks[4].y=leftY;c.subjects[0].landmarks[4].visible=left;
            c.subjects[0].landmarks[5].y=rightY;c.subjects[0].landmarks[5].visible=right;
            Require(Fusion.ObserveCamera(c,At));if(Game.Game==JungleGame.LogBalance)Game.UpdatePhone(p,At);else Game.Update(Fusion.EvaluateLatest(1,At),At);
        }
        public void Warm() { Step(1000);Step(1050);Step(1100);Require(Game.Ready); }
        public bool Use(JungleAction action) => Game.Current!=null&&Game.Authorize(action,Game.Current.Revision,At);
        public void Lost()
        {
            var c=Checks.Camera(++CameraSequence);c.captureTimestamp=At;c.processTimestamp=At+1;c.subjects=Array.Empty<CameraSubject>();c.trackingState="LOST";
            Fusion.ObserveCamera(c,At+2);At+=2;if(Game.Game==JungleGame.LogBalance)Game.UpdatePhone(null,At);else Game.Update(Fusion.EvaluateLatest(1,At),At);
        }
    }
    public static IEnumerable<(string,Action)> All()
    {
        yield return ("brief camera gaps allow stable recovery without resuming on a missing frame",()=>{
            var gate=new TrackingPauseStability();
            Require(gate.Update(true,0,.18f));
            Require(gate.Update(false,.2f,.18f));
            Require(gate.Update(true,.25f,.18f));
            Require(gate.Update(false,.45f,.18f));
            Require(gate.Update(false,.51f,.18f));
            Require(!gate.Update(true,.55f,.18f));
            Require(!gate.Update(false,.6f,.18f));
            Require(gate.Update(false,.8f,.18f));
            Require(gate.Update(true,.81f,.18f));
        });
        yield return ("camera expiry gates actions while brief delivery gaps retain association",()=>{
            var l=new Lab(JungleGame.Bowling);l.Warm();
            var p=Checks.Phone(++l.Sequence);p.serverReceiveTimestamp=1360;
            Require(l.Phones.Accept(p));l.Fusion.ObservePhone(p);
            var expired=l.Fusion.EvaluateLatest(1,1360);
            Require(!expired.Eligible&&expired.LockState==PlayerLockState.LOCKED);
            l.Game.Update(expired,1360);Require(!l.Game.Ready);
            var c=Checks.Camera(++l.CameraSequence);c.captureTimestamp=1240;c.processTimestamp=1361;
            Require(l.Fusion.ObserveCamera(c,1361));
            Require(l.Fusion.Association.State==PlayerLockState.LOCKED);
            l.Fusion.EvaluateLatest(1,1700);Require(l.Fusion.Association.State==PlayerLockState.LOST);
        });
        yield return ("technical pause freezes and restores the original time scale",()=>{var p=new JunglePauseLease();Require(p.Apply(true,.5f)==0&&p.Owned);Require(p.Apply(true,0)==0);Require(p.Apply(false,0)==.5f&&!p.Owned);});
        yield return ("technical pause never resumes another system's existing pause",()=>{var p=new JunglePauseLease();Require(p.Apply(true,0)==0&&!p.Owned);Require(p.Apply(false,0)==0);});
        yield return ("leaving a game releases only its own pause",()=>{var p=new JunglePauseLease();p.Apply(true,1);Require(p.Apply(false,0)==1);p.Apply(true,1);Require(p.Apply(false,.25f)==.25f);});
        yield return ("tracking pause stability allows short camera grace and requires stable resume",()=>{
            var gate=new TrackingPauseStability();
            Require(gate.Update(true,0,.18f));Require(!gate.Update(true,.51f,.18f));
            Require(!gate.Update(false,.6f,.18f));Require(!gate.Update(true,.7f,.18f));
            Require(!gate.Update(false,.8f,.18f));Require(gate.Update(false,1f,.18f));
        });
        yield return ("visual hand prediction is bounded and expires without mutating input",()=>{
            var visual=new HandVisualPrediction();Require(!visual.TryPosition(1,out _,out _));
            visual.Observe(.5f,.5f,4f,0f,1f);Require(visual.TryPosition(1.04f,out var x,out var y)&&Math.Abs(x-.5f)<.0001f&&Math.Abs(y-.5f)<.0001f);
            visual.Observe(.8f,.5f,4f,0f,1.1f);Require(visual.TryPosition(1.14f,out x,out y)&&x>.7f&&x<=.825f);
            Require(!visual.TryPosition(1.4f,out _,out _));
        });
        yield return ("visual cursor damps stationary jitter and resets cleanly after tracking loss",()=>{
            var visual=new HandVisualPrediction();visual.Observe(.5f,.5f,0,0,0);
            for(int i=1;i<=60;i++){
                visual.Observe(.5f+(i%2==0?.006f:-.006f),.5f,0,0,i*.04f);
                Require(visual.TryPosition(i*.04f,out var x,out _)&&Math.Abs(x-.5f)<.004f);
            }
            visual.Reset();Require(!visual.TryPosition(2.41f,out _,out _));
            visual.Observe(.9f,.2f,0,0,2.5f);Require(visual.TryPosition(2.5f,out var next,out _)&&Math.Abs(next-.9f)<.0001f);
        });
        yield return ("bowling hook and slice precision mechanics have bounded curves",()=>{
            Require(ShotMechanics.BowlingSpin(0)==0&&ShotMechanics.BowlingSpin(4)==0);
            Require(Math.Abs(ShotMechanics.BowlingSpin(25)-.5f)<.001f&&Math.Abs(ShotMechanics.BowlingSpin(-25)+.5f)<.001f);
            Require(ShotMechanics.BowlingCurveAcceleration(-1)<0&&ShotMechanics.BowlingCurveAcceleration(1)>0);
            Require(Math.Abs(ShotMechanics.SlicePrecisionMultiplier(0,.075f)-1.5f)<.001f&&Math.Abs(ShotMechanics.SlicePrecisionMultiplier(.075f,.075f)-1f)<.001f);
        });
        yield return ("camera framing report classifies safe zone, missing hands and raised hand",()=>{
            var c=Checks.Camera(1);
            var report=ShotMechanics.EvaluateFraming(true,c,true);
            Require(report.Status==CameraFramingStatus.Optimal&&report.InSafeZone&&!report.HandRaised);
            c.subjects[0].landmarks[5].y=.15f; // right wrist above shoulder (.3f)
            report=ShotMechanics.EvaluateFraming(true,c,true);
            Require(report.Status==CameraFramingStatus.Optimal&&report.HandRaised);
            c.subjects[0].landmarks[4].visible=false;
            report=ShotMechanics.EvaluateFraming(true,c,true);
            Require(report.Status==CameraFramingStatus.MissingHands&&!report.InSafeZone);
        });
        yield return ("comfortable camera range reaches all screen corners without changing handedness",()=>{
            HandCursorMapping.Map(.18f,.15f,out var x,out var y);Require(x==0&&y==0);
            HandCursorMapping.Map(.82f,.15f,out x,out y);Require(x>.999f&&y==0);
            HandCursorMapping.Map(.18f,.85f,out x,out y);Require(x==0&&y>.999f);
            HandCursorMapping.Map(.82f,.85f,out x,out y);Require(x>.999f&&y>.999f);
            HandCursorMapping.Map(.5f,.5f,out x,out y);Require(Math.Abs(x-.5f)<.001f&&Math.Abs(y-.5f)<.001f);
            HandCursorMapping.Map(0,1,out x,out y);Require(x==0&&y==1);
            var navigator=new HandDwellNavigator();var targets=new[]{new HandDwellTarget(1,0,0,100,100),new HandDwellTarget(2,900,0,100,100)};
            HandCursorMapping.Map(.18f,.15f,out x,out y);
            Require(!navigator.ObserveCursor(true,x*1000,y*1000,0,targets,out _)&&navigator.HoveredId==1);
            Require(!navigator.ObserveCursor(false,0,0,.5f,targets,out _)&&navigator.HoveredId==-1);
            HandCursorMapping.Map(.82f,.15f,out x,out y);
            Require(!navigator.ObserveCursor(true,x*1000,y*1000,1,targets,out _)&&navigator.HoveredId==2);
        });
        yield return ("hand dwell navigator selects hovered target once and tolerates brief edge jitter",()=>{
            var nav=new HandDwellNavigator();
            var targets=new[]{new HandDwellTarget(7,100,100,200,100)};
            Require(!nav.ObserveCursor(true,150,150,0f,targets,out _,1f)&&nav.HoveredId==7);
            Require(!nav.ObserveCursor(true,150,150,.20f,targets,out _,1f));
            Require(!nav.ObserveCursor(true,50,50,.28f,targets,out _,1f)&&nav.HoveredId==7); // within edge grace
            for(int i=1;i<=5;i++)nav.ObserveCursor(true,150,150,.28f+i*.16f,targets,out _,1f);
            Require(nav.ObserveCursor(true,150,150,1.25f,targets,out int hit,1f)&&hit==7);
            Require(!nav.ObserveCursor(true,150,150,1.45f,targets,out _,1f)); // latched while inside
        });
        yield return ("goalie save mechanics evaluate both hands and torso with authorized save action",()=>{
            var leftSave=ShotMechanics.EvaluateGoalieSave(.25f,.75f,true,.25f,.75f,.8f,true,.75f,.75f,0f,true,.5f,.5f);
            Require(leftSave.Saved&&leftSave.Source==GoalieSaveSource.LeftHand&&Math.Abs(leftSave.PrecisionMultiplier-1.5f)<.001f);
            var torsoSave=ShotMechanics.EvaluateGoalieSave(.5f,.45f,true,.1f,.8f,0f,true,.9f,.8f,0f,true,.5f,.45f);
            Require(torsoSave.Saved&&torsoSave.Source==GoalieSaveSource.Torso);
            var miss=ShotMechanics.EvaluateGoalieSave(.1f,.1f,true,.5f,.5f,0f,true,.6f,.5f,0f,true,.5f,.4f);
            Require(!miss.Saved&&miss.Source==GoalieSaveSource.None);
            var l=new Lab(JungleGame.CoconutSlice);l.Warm();l.Step(1150,0,.4f,.4f);
            Require(l.Use(JungleAction.Save)&&!l.Use(JungleAction.Save));
        });
        yield return ("manual camera reacquisition requires fresh frames and never replays an action",()=>{
            var l=new Lab(JungleGame.CoconutSlice);l.Warm();l.Step(1150,0,.4f,.4f);var old=l.Game.Current;Require(old!=null);
            l.Lost();l.Step(1200,0,.4f,.4f,subject:2);l.Step(1250,0,.4f,.4f,subject:2);
            Require(!l.Game.Ready&&l.Fusion.Association.SubjectId==1&&!l.Game.Authorize(JungleAction.SliceRight,old.Revision,l.At));
            l.Fusion.Association.Ready(1);l.Game.Select(l.Game.Game);
            l.Step(1300,0,.6f,.6f,subject:2);Require(!l.Game.Ready);
            l.Step(1350,0,.6f,.6f,subject:2);Require(l.Game.Ready&&l.Game.Current.Baseline&&l.Game.Current.Actions==JungleAction.None);
            Require(!l.Game.Authorize(JungleAction.SliceRight,old.Revision,l.At));
        });
        yield return ("all seven gameplay scenes have an explicit fusion profile",()=>{
            var scenes=new[]{"BananaBowling","GorillaBoxing","JungleBaseball","JungleDrums","CoconutSlice","WhackAMole","LogBalance"};
            foreach(var s in scenes)Require(JungleGameplayInput.ForScene(s)!=JungleGame.None);
            Require(JungleGameplayInput.ForScene("Connection")==JungleGame.None&&JungleGameplayInput.ForScene("MainMenu")==JungleGame.None);
        });
        foreach(var kind in new[]{JungleGame.Bowling,JungleGame.Boxing,JungleGame.Baseball,JungleGame.Drums,JungleGame.CoconutSlice,JungleGame.Whack})
        {
            var game=kind;
            yield return ($"{game}: tracking loss immediately invalidates prior actions",()=>{var l=new Lab(game);l.Warm();l.Step(1150,500,.4f,.4f);var frame=l.Game.Current;Require(frame!=null);l.Lost();Require(!l.Game.Ready&&l.Game.Current==null);Require(!l.Game.Authorize(JungleAction.Throw,frame.Revision,l.At));});
            yield return ($"{game}: baseline never creates a scoring gesture",()=>{var l=new Lab(game);l.Step(1000,500,.4f,.4f);l.Step(1050,500,.6f,.6f);Require(l.Game.Ready&&l.Game.Current.Baseline&&l.Game.Current.Actions==JungleAction.None);});
            yield return ($"{game}: missing required right hand blocks gameplay",()=>{var l=new Lab(game);l.Warm();l.Step(1150,500,.4f,.4f,right:false);Require(!l.Game.Ready);});
            yield return ($"{game}: a new phone epoch rebaselines without a false action",()=>{var l=new Lab(game);l.Warm();l.Step(1150,500,.6f,.6f,epoch:2);Require(l.Game.Ready&&l.Game.Current.Baseline&&l.Game.Current.Actions==JungleAction.None);});
        }
        foreach(var kind in new[]{JungleGame.Boxing,JungleGame.Drums,JungleGame.CoconutSlice})
        {
            var game=kind;
            yield return ($"{game}: left hand is required instead of invented",()=>{var l=new Lab(game);l.Warm();l.Step(1150,500,.4f,.4f,left:false);Require(!l.Game.Ready);});
        }
        yield return ("Tronco: phone works without a camera and rejects stale data",()=>{
            var game=new JungleGameplayInput();game.Select(JungleGame.LogBalance);
            var first=Checks.Phone(1);first.serverReceiveTimestamp=1000;game.UpdatePhone(first,1000);
            Require(game.Ready&&game.Current.Baseline&&!JungleGameplayInput.RequiresCamera(JungleGame.LogBalance));
            var next=Checks.Phone(2);next.serverReceiveTimestamp=1050;game.UpdatePhone(next,1050);
            Require(game.Ready&&game.Authorize(JungleAction.Balance,game.Current.Revision,1050));
            game.UpdatePhone(next,1401);Require(!game.Ready&&game.Current==null);
        });
        yield return ("Tronco: accepts gamma and screen without unrelated motion axes",()=>{
            var game=new JungleGameplayInput();game.Select(JungleGame.LogBalance);
            var phone=Checks.Phone(1);phone.serverReceiveTimestamp=1000;phone.rotationRate=null;phone.accelerationIncludingGravity=null;
            phone.orientation.hasAlpha=false;phone.orientation.hasBeta=false;game.UpdatePhone(phone,1000);Require(game.Ready);
        });
        yield return ("Tronco: disconnection and incomplete sensor input pause safely",()=>{
            var game=new JungleGameplayInput();game.Select(JungleGame.LogBalance);
            var phone=Checks.Phone(1);phone.serverReceiveTimestamp=1000;game.UpdatePhone(phone,1000);Require(game.Ready);
            phone.connectionState="DISCONNECTED";game.UpdatePhone(phone,1001);Require(!game.Ready);
            var partial=Checks.Phone(2);partial.serverReceiveTimestamp=1050;partial.orientation.hasGamma=false;
            game.UpdatePhone(partial,1050);Require(!game.Ready&&game.Status==JungleInputStatus.PartialInput);
        });
        yield return ("Bowling: launch requires both real wrist movement and phone peak",()=>{
            var l=new Lab(JungleGame.Bowling);l.Warm();l.Step(1150,400);Require(!l.Use(JungleAction.Throw));
            l.Step(1200);l.Step(1250,400,.4f);Require(l.Use(JungleAction.Throw));Require(!l.Use(JungleAction.Throw));
        });
        yield return ("Boxing: left and right punches have independent validated gestures",()=>{
            var l=new Lab(JungleGame.Boxing);l.Warm();l.Step(1150,0,.3f,.4f);Require(l.Use(JungleAction.PunchLeft)&&!l.Use(JungleAction.PunchRight));
            l.Step(1200,400,.4f,.4f);Require(l.Use(JungleAction.PunchRight));
        });
        yield return ("Boxing: stand-up pulse also requires tracked motion",()=>{
            var l=new Lab(JungleGame.Boxing);l.Warm();l.Step(1150,400);Require(!l.Use(JungleAction.Recover));l.Step(1200);l.Step(1250,400,.4f);Require(l.Use(JungleAction.Recover));
        });
        yield return ("Baseball: old hand speed cannot turn a new phone peak into a swing",()=>{
            var l=new Lab(JungleGame.Baseball);l.Warm();l.Step(1150,0,.4f);l.Step(1200,0,.4f);l.Step(1250,400,.4f);Require(!l.Use(JungleAction.Swing));
            l.Step(1300,0,.4f);l.Step(1350,400,.5f);Require(l.Use(JungleAction.Swing));
        });
        yield return ("Drums: simultaneous hands are authorized once each",()=>{
            var l=new Lab(JungleGame.Drums);l.Warm();l.Step(1150,400,.4f,.4f);Require(l.Use(JungleAction.DrumLeft)&&l.Use(JungleAction.DrumRight));Require(!l.Use(JungleAction.DrumLeft)&&!l.Use(JungleAction.DrumRight));
        });
        yield return ("Coconut Slice: actual capture interval determines cutting speed",()=>{
            var l=new Lab(JungleGame.CoconutSlice);l.Warm();l.Step(1150,0,.4f,.4f);Require(Math.Abs(l.Game.Current.Right.Speed-2)<.001&&l.Use(JungleAction.SliceRight));
        });
        yield return ("Whack: phone shake without downward hand motion never hits",()=>{
            var l=new Lab(JungleGame.Whack);l.Warm();l.Step(1150,400);Require(!l.Use(JungleAction.Whack));l.Step(1200);l.Step(1250,400,.4f);Require(l.Use(JungleAction.Whack));
        });
        yield return ("held peak cannot repeatedly fire after cooldown",()=>{
            var l=new Lab(JungleGame.Bowling);l.Warm();l.Step(1150,400,.4f);Require(l.Use(JungleAction.Throw));
            for(int i=0;i<20;i++){l.Step(1200+i*50,400,.4f+i*.001f);Require(!l.Use(JungleAction.Throw));}
        });
        yield return ("recovery rebaselines before computing wrist velocity",()=>{
            var l=new Lab(JungleGame.Bowling);l.Warm();l.Lost();l.Step(1200,400,.8f);Require(!l.Game.Ready);l.Step(1250,400,.8f);Require(l.Game.Ready&&l.Game.Current.Baseline&&l.Game.Current.Right.Speed==0&&!l.Use(JungleAction.Throw));
            l.Step(1300,400,.7f);Require(!l.Use(JungleAction.Throw));l.Step(1350,0,.7f);l.Step(1400,400,.6f);Require(l.Use(JungleAction.Throw));
        });
        yield return ("partial current input cannot reuse a complete historical sample",()=>{var l=new Lab(JungleGame.Bowling);l.Warm();l.Step(1150,500,.4f,partial:true);Require(!l.Game.Ready&&l.Game.Status==JungleInputStatus.PartialInput);});
        yield return ("disconnection invalidates the frame without waiting for a camera event",()=>{
            var l=new Lab(JungleGame.Bowling);l.Warm();var p=Checks.Phone(l.Sequence);p.connectionState="DISCONNECTED";p.messageType="SERVER_STATE";
            Require(l.Phones.Accept(p));l.Game.Update(l.Fusion.EvaluateLatest(1,l.At),l.At);Require(!l.Game.Ready);
        });
        yield return ("current camera rejects obsolete minimum-skew diagnostic match",()=>{
            var l=new Lab(JungleGame.Bowling);l.Warm();
            var p=Checks.Phone(++l.Sequence);p.serverReceiveTimestamp=1150;l.Phones.Accept(p);l.Fusion.ObservePhone(p);
            var c=Checks.Camera(++l.CameraSequence);c.captureTimestamp=1260;c.processTimestamp=1261;l.Fusion.ObserveCamera(c,1262);
            Require(l.Fusion.Evaluate(1,1262).Eligible);Require(!l.Fusion.EvaluateLatest(1,1262).Eligible);
        });
        yield return ("Bowling requires only the selected throwing hand, including low overall confidence",()=>{
            foreach(bool left in new[]{false,true}){
                var l=new Lab(JungleGame.Bowling);l.Game.LeftHanded=left;
                for(int i=0;i<4;i++){
                    long now=1000+i*50;var p=Checks.Phone(++l.Sequence);p.serverReceiveTimestamp=now;
                    Require(l.Phones.Accept(p));l.Fusion.ObservePhone(p);
                    var c=Checks.Camera(++l.CameraSequence);c.captureTimestamp=now;c.processTimestamp=now+1;
                    c.subjects[0].confidence=.1f;
                    c.subjects[0].landmarks[left?5:4].visible=false;
                    Require(l.Fusion.ObserveCamera(c,now+2,true));
                    l.Game.Update(l.Fusion.EvaluateLatest(1,now+2),now+2);
                }
                Require(l.Game.Ready);
                var lost=Checks.Camera(++l.CameraSequence);lost.captureTimestamp=1200;lost.processTimestamp=1201;
                lost.subjects[0].landmarks[left?4:5].visible=false;
                l.Fusion.ObserveCamera(lost,1202,true);l.Game.Update(l.Fusion.EvaluateLatest(1,1202),1202);Require(!l.Game.Ready);
            }
        });
        yield return ("camera inference latency matches observed phone history without pausing",()=>{
            var l=new Lab(JungleGame.Bowling);l.Warm();
            for(long now=1120;now<=2320;now+=20){
                var p=Checks.Phone(++l.Sequence);p.serverReceiveTimestamp=now;
                Require(l.Phones.Accept(p));l.Fusion.ObservePhone(p);
                if(now>=1260&&now%100==60){
                    var c=Checks.Camera(++l.CameraSequence);c.captureTimestamp=now-140;c.processTimestamp=now;
                    Require(l.Fusion.ObserveCamera(c,now));
                }
                var candidate=l.Fusion.EvaluateLatest(1,now);
                l.Game.Update(candidate,now);
                Require(l.Game.Ready&&candidate.AlignmentDelta<=20);
                Require(l.Game.Current.CameraSequence==l.Fusion.Cameras.Latest.frameSequence);
                Require(l.Game.Current.PhoneSequence<=p.sequence);
            }
        });
        yield return ("buffered phone match never conceals missing current sensors",()=>{
            var l=new Lab(JungleGame.Bowling);l.Warm();
            var p=Checks.Phone(++l.Sequence);p.serverReceiveTimestamp=1240;p.orientation.hasBeta=false;
            Require(l.Phones.Accept(p));l.Fusion.ObservePhone(p);
            var c=Checks.Camera(++l.CameraSequence);c.captureTimestamp=1100;c.processTimestamp=1240;
            Require(l.Fusion.ObserveCamera(c,1240));
            l.Game.Update(l.Fusion.EvaluateLatest(1,1240),1240);
            Require(!l.Game.Ready&&l.Game.Status==JungleInputStatus.PartialInput);
        });
        yield return ("buffered phone match never conceals disconnection or stale camera",()=>{
            var l=new Lab(JungleGame.Bowling);l.Warm();
            var p=Checks.Phone(++l.Sequence);p.serverReceiveTimestamp=1240;
            Require(l.Phones.Accept(p));l.Fusion.ObservePhone(p);
            l.Game.Update(l.Fusion.EvaluateLatest(1,1240),1240);Require(l.Game.Ready);
            p.connectionState="DISCONNECTED";
            l.Game.Update(l.Fusion.EvaluateLatest(1,1241),1241);Require(!l.Game.Ready);
            p.connectionState="ACTIVE";
            p=Checks.Phone(++l.Sequence);p.serverReceiveTimestamp=1400;
            Require(l.Phones.Accept(p));l.Fusion.ObservePhone(p);
            l.Game.Update(l.Fusion.EvaluateLatest(1,1400),1400);Require(!l.Game.Ready);
        });
        yield return ("a second subject immediately invalidates gameplay association",()=>{
            var l=new Lab(JungleGame.Bowling);l.Warm();var c=Checks.Camera(++l.CameraSequence);c.captureTimestamp=1150;c.processTimestamp=1151;
            var s=c.subjects[0];c.subjects=new[]{s,new CameraSubject{subjectId=2,centerX=.5f,centerY=.5f,confidence=.9f,landmarks=s.landmarks}};
            l.Fusion.ObserveCamera(c,1152);l.Game.Update(l.Fusion.EvaluateLatest(1,1152),1152);Require(!l.Game.Ready);
        });
        yield return ("scene transition invalidates outstanding gesture authorization",()=>{var l=new Lab(JungleGame.Bowling);l.Warm();l.Step(1150,400,.4f);long revision=l.Game.Current.Revision;l.Game.Select(JungleGame.Boxing);Require(!l.Game.Authorize(JungleAction.Throw,revision,l.At));});
        yield return ("freshness is checked again at action authorization",()=>{var l=new Lab(JungleGame.Bowling);l.Warm();l.Step(1150,400,.4f);Require(!l.Game.Authorize(JungleAction.Throw,l.Game.Current.Revision,l.At+300));});
        yield return ("same accepted pair never generates a second frame or repeated action",()=>{
            var l=new Lab(JungleGame.Bowling);l.Warm();l.Step(1150,400,.4f);long revision=l.Game.Current.Revision;Require(l.Use(JungleAction.Throw));
            l.Game.Update(l.Fusion.EvaluateLatest(1,l.At+10),l.At+10);Require(l.Game.Current.Revision==revision&&!l.Use(JungleAction.Throw));
        });
        yield return ("an action mask cannot authorize combined or unrelated gestures",()=>{
            var l=new Lab(JungleGame.Bowling);l.Warm();l.Step(1150,400,.4f);Require(!l.Use(JungleAction.Throw|JungleAction.Whack)&&!l.Use(JungleAction.PunchLeft));Require(l.Use(JungleAction.Throw));
        });
        yield return ("implausible derived numeric overflow is rejected",()=>{
            var l=new Lab(JungleGame.Bowling);l.Warm();l.Step(1150,float.MaxValue,.4f);Require(!l.Game.Ready&&l.Game.Status==JungleInputStatus.PartialInput);
        });
        yield return ("unknown screen orientation does not become a fabricated zero",()=>{
            var l=new Lab(JungleGame.Bowling);l.Warm();var p=Checks.Phone(++l.Sequence);p.serverReceiveTimestamp=1150;p.screenOrientation.hasAngle=false;l.Phones.Accept(p);l.Fusion.ObservePhone(p);
            var c=Checks.Camera(++l.CameraSequence);c.captureTimestamp=1150;c.processTimestamp=1151;l.Fusion.ObserveCamera(c,1152);l.Game.Update(l.Fusion.EvaluateLatest(1,1152),1152);Require(!l.Game.Ready&&l.Game.Status==JungleInputStatus.PartialInput);
        });
        yield return ("a tracking coordinate teleport is discarded before gesture recognition",()=>{
            var l=new Lab(JungleGame.Bowling);l.Warm();l.Step(1110,400,.95f);Require(!l.Game.Ready&&l.Game.Status==JungleInputStatus.Reacquiring);
        });
        yield return ("long runs keep bounded camera and input histories",()=>{
            var l=new Lab(JungleGame.CoconutSlice);l.Warm();for(int i=0;i<10000;i++)l.Step(1150+i*20);
            Require(l.Game.Ready&&l.Fusion.Cameras.Frames.Count<=16&&l.Fusion.RetainedPhoneSamples(1)<=32&&l.Phones.Actions.Count==0);
        });
    }
}
