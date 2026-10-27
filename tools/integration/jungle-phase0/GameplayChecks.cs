using System;
using System.Collections.Generic;
using GorillaEscape.Contracts;
using GorillaEscape.Input;
using GorillaEscape.Input.Fusion;
using GorillaEscape.Gameplay.Input;

static class GameplayChecks
{
    static void Require(bool pass) { if(!pass)throw new Exception("assertion failed"); }
    sealed class Lab
    {
        public readonly PhoneInputStore Phones=new PhoneInputStore();
        public readonly InputFusion Fusion;
        public readonly JungleGameplayInput Game=new JungleGameplayInput();
        public long At,Sequence,CameraSequence;
        public Lab(JungleGame game) {Fusion=new InputFusion(Phones,new FusionSettings());Fusion.Association.Ready(1);Game.Select(game);}
        public void Step(long at,float angular=0,float rightY=.3f,float leftY=.3f,bool right=true,bool left=true,long epoch=1,bool partial=false)
        {
            At=at+2;
            var p=Checks.Phone(++Sequence);p.serverReceiveTimestamp=at;p.connectionEpoch=epoch;
            p.rotationRate.alpha=angular;p.rotationRate.beta=p.rotationRate.gamma=0;
            p.accelerationIncludingGravity.x=p.accelerationIncludingGravity.y=0;p.accelerationIncludingGravity.z=9.81f;
            if(partial)p.orientation.hasBeta=false;
            Require(Phones.Accept(p));Fusion.ObservePhone(p);
            var c=Checks.Camera(++CameraSequence);c.captureTimestamp=at;c.processTimestamp=at+1;
            c.subjects[0].landmarks[4].y=leftY;c.subjects[0].landmarks[4].visible=left;
            c.subjects[0].landmarks[5].y=rightY;c.subjects[0].landmarks[5].visible=right;
            Require(Fusion.ObserveCamera(c,At));Game.Update(Fusion.EvaluateLatest(1,At),At);
        }
        public void Warm() { Step(1000);Step(1050);Step(1100);Require(Game.Ready); }
        public bool Use(JungleAction action) => Game.Current!=null&&Game.Authorize(action,Game.Current.Revision,At);
        public void Lost()
        {
            var c=Checks.Camera(++CameraSequence);c.captureTimestamp=At;c.processTimestamp=At+1;c.subjects=Array.Empty<CameraSubject>();c.trackingState="LOST";
            Fusion.ObserveCamera(c,At+2);At+=2;Game.Update(Fusion.EvaluateLatest(1,At),At);
        }
    }
    public static IEnumerable<(string,Action)> All()
    {
        yield return ("technical pause freezes and restores the original time scale",()=>{var p=new JunglePauseLease();Require(p.Apply(true,.5f)==0&&p.Owned);Require(p.Apply(true,0)==0);Require(p.Apply(false,0)==.5f&&!p.Owned);});
        yield return ("technical pause never resumes another system's existing pause",()=>{var p=new JunglePauseLease();Require(p.Apply(true,0)==0&&!p.Owned);Require(p.Apply(false,0)==0);});
        yield return ("leaving a game releases only its own pause",()=>{var p=new JunglePauseLease();p.Apply(true,1);Require(p.Apply(false,0)==1);p.Apply(true,1);Require(p.Apply(false,.25f)==.25f);});
        yield return ("all six gameplay scenes have an explicit fusion profile",()=>{
            var scenes=new[]{"BananaBowling","GorillaBoxing","JungleBaseball","JungleDrums","CoconutSlice","WhackAMole"};
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
        yield return ("current pair rejects obsolete minimum-skew diagnostic match",()=>{
            var l=new Lab(JungleGame.Bowling);l.Warm();
            var p=Checks.Phone(++l.Sequence);p.serverReceiveTimestamp=1150;l.Phones.Accept(p);l.Fusion.ObservePhone(p);
            var c=Checks.Camera(++l.CameraSequence);c.captureTimestamp=1260;c.processTimestamp=1261;l.Fusion.ObserveCamera(c,1262);
            Require(l.Fusion.Evaluate(1,1262).Eligible);Require(!l.Fusion.EvaluateLatest(1,1262).Eligible);
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
