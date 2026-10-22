using System.Collections;
using GorillaEscape.Minigames.LogBalance;
using GorillaEscape.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class LogBalanceFlowTests
{
    [UnityTest] public IEnumerator PhoneOnboardingReturnsToTrunkAndSelectsReadyPlayer()
    {
        var host=new GameObject("Phone onboarding return test");var menu=host.AddComponent<MainMenuController>();yield return null;
        try{
            menu.OpenLogBalance();menu.OpenPhoneForLogBalance();Assert.True(menu.InPhone);Assert.IsNull(menu.LogBalance);
            var field=typeof(GorillaEscape.QA.QaRuntimeService).GetField("java",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            var java=(GorillaEscape.Network.JavaProbeSupervisor)field.GetValue(menu.Runtime);
            string session=System.Guid.NewGuid().ToString(),device=System.Guid.NewGuid().ToString();
            var player=new GorillaEscape.Contracts.LobbyPlayerWire{playerId=2,deviceSessionId=device,connectionEpoch=1,connectionState="ACTIVE",networkReady=true,inputReady=false};
            var lobby=new GorillaEscape.Contracts.LobbySnapshotWire{lobbyVersion=1,sessionId=session,phase="OPEN",players=new[]{player}};
            java.Lobby.Accept(lobby,null);Assert.False(menu.CompletePhoneConnection(),"Network alone must not finish onboarding");
            player.inputReady=true;java.Lobby.Accept(lobby,null);
            Assert.False(menu.CompletePhoneConnection(),"Fresh phone input is required");
            java.PhoneInputs.Accept(new GorillaEscape.Contracts.PhoneInputWire{
                playerId=2,sessionId=session,deviceSessionId=device,sequence=1,connectionEpoch=1,
                serverReceiveTimestamp=System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),messageType="MOTION_SAMPLE",connectionState="ACTIVE",
                quality=new GorillaEscape.Contracts.PhoneQuality{source="synthetic",status="available"},
                orientation=new GorillaEscape.Contracts.PhoneValues{hasGamma=true,gamma=10,availability="partial"},
                screenOrientation=new GorillaEscape.Contracts.PhoneValues{hasAngle=true,angle=0,availability="present"}});
            Assert.True(menu.CompletePhoneConnection());Assert.False(menu.InPhone);Assert.NotNull(menu.LogBalance);
            Assert.AreEqual(2,menu.LogBalance.Player);Assert.False(menu.LogBalance.Keyboard);
            var game=menu.LogBalance;game.SendMessage("OnApplicationFocus",true);
            Assert.False(game.BeginAttempt(),"Starting queues while the automatic neutral is collected");
            for(int i=0;i<12;i++){
                java.PhoneInputs.Accept(new GorillaEscape.Contracts.PhoneInputWire{
                    playerId=2,sessionId=session,deviceSessionId=device,sequence=i+2,connectionEpoch=1,
                    serverReceiveTimestamp=System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),messageType="MOTION_SAMPLE",connectionState="ACTIVE",
                    quality=new GorillaEscape.Contracts.PhoneQuality{source="synthetic",status="available"},
                    orientation=new GorillaEscape.Contracts.PhoneValues{hasGamma=true,gamma=10,availability="partial"},
                    screenOrientation=new GorillaEscape.Contracts.PhoneValues{hasAngle=true,angle=0,availability="present"}});
                yield return new WaitForSecondsRealtime(.06f);
            }
            Assert.That(game.Attempt.State,Is.EqualTo(LogBalanceState.Countdown),"One start press must continue automatically without a calibration button");
            game.RestartAttempt();Assert.False(game.BeginAttempt());
            for(int i=0;i<12;i++){
                java.PhoneInputs.Accept(new GorillaEscape.Contracts.PhoneInputWire{
                    playerId=2,sessionId=session,deviceSessionId=device,sequence=i+14,connectionEpoch=1,
                    serverReceiveTimestamp=System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),messageType="MOTION_SAMPLE",connectionState="ACTIVE",
                    quality=new GorillaEscape.Contracts.PhoneQuality{source="synthetic",status="available"},
                    orientation=new GorillaEscape.Contracts.PhoneValues{hasGamma=true,gamma=-12,availability="partial"},
                    screenOrientation=new GorillaEscape.Contracts.PhoneValues{hasAngle=true,angle=0,availability="present"}});
                yield return new WaitForSecondsRealtime(.06f);
            }
            Assert.That(game.Attempt.State,Is.EqualTo(LogBalanceState.Countdown),"Retry must learn a changed comfortable posture automatically");
            menu.CloseLogBalance();
        }finally{Object.Destroy(host);}yield return null;
    }
    [UnityTest] public IEnumerator TrunkFollowsSimulatedRollAndLateralFootingInsteadOfElapsedTime()
    {
        var host=new GameObject("Rolling log test");var stage=new LogBalanceStage(host.transform,18);
        try{
            var pivot=stage.Root.transform.Find("Rolling trunk pivot");
            stage.Present(.3f,0,false,0,0,true);var start=pivot.localRotation;
            stage.Present(.3f,.6f,false,0,2,true,false,36,1);
            Assert.That(Quaternion.Angle(start,pivot.localRotation),Is.EqualTo(36).Within(.01));
            var human=stage.Root.transform.Find("Human castaway — controlled");Assert.False(float.IsNaN(human.localPosition.y));Assert.That(human.localPosition.x,Is.EqualTo(.252f).Within(.001));
            var paused=pivot.localRotation;stage.Present(.3f,.6f,false,0,2,false,false,36,1);
            Assert.That(Quaternion.Angle(paused,pivot.localRotation),Is.LessThan(.01));
            stage.Present(0,0,false,0,0,false);Assert.That(Quaternion.Angle(start,pivot.localRotation),Is.LessThan(.01));
        }finally{stage.Dispose();Object.Destroy(host);}
        yield return null;
    }
    [UnityTest] public IEnumerator PrototypeKeepsServicesAndMenuOwnershipAndCanRestart()
    {
        var host=new GameObject("Log Balance test menu");var menu=host.AddComponent<MainMenuController>();
        yield return null;
        try
        {
            var runtime=menu.Runtime;
            menu.OpenLogBalance();var game=menu.LogBalance;Assert.NotNull(game);
            Assert.False(menu.PlayEnabled);Assert.False(runtime.ServicesEnabled);
            Assert.NotNull(GameObject.Find("Human castaway — controlled"));
            Assert.NotNull(GameObject.Find("Gorilla spectator — NPC"));
            Assert.False(game.BeginAttempt(),"Phone is primary and cannot start without admission/calibration");
            game.SelectKeyboard(true);Assert.True(game.Keyboard);Assert.True(game.BeginAttempt());
            game.PauseAttempt();Assert.True(game.Paused);
            var m=game.Attempt;m.Tick(.5f,0);game.RestartAttempt();
            Assert.That(m.State,Is.EqualTo(LogBalanceState.Ready));Assert.That(m.Result,Is.Null);
            Assert.That(m.Elapsed,Is.Zero);Assert.True(game.BeginAttempt());
            for(int i=0;i<1500;i++)m.Tick(.02f,-m.Balance*1.9f-m.LateralVelocity*.85f-m.LogAngle*.8f-m.LogAngularVelocity*.25f-.04f);
            Assert.That(m.State,Is.EqualTo(LogBalanceState.Won));
            game.RestartAttempt();Assert.True(game.BeginAttempt());
            for(int i=0;i<1500;i++)m.Tick(.02f,1);
            Assert.That(m.State,Is.EqualTo(LogBalanceState.Fell));
            menu.CloseLogBalance();Assert.IsNull(menu.LogBalance);
            Assert.AreSame(runtime,menu.Runtime);Assert.True(runtime.CleanupComplete);
            yield return null;
            Assert.IsNull(GameObject.Find("Human castaway — controlled"));
            menu.OpenLogBalance();Assert.That(menu.LogBalance.Attempt.State,Is.EqualTo(LogBalanceState.Ready));
            menu.CloseLogBalance();
        }
        finally {Object.Destroy(host);}
        yield return null;
    }
    [UnityTest] public IEnumerator PhoneReplayUsesAdmittedIdentityAndPausesOnLoss()
    {
        var host=new GameObject("Log Balance phone fixture");var menu=host.AddComponent<MainMenuController>();
        yield return null;
        try
        {
            var field=typeof(GorillaEscape.QA.QaRuntimeService).GetField("java",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            var java=(GorillaEscape.Network.JavaProbeSupervisor)field.GetValue(menu.Runtime);
            string session=System.Guid.NewGuid().ToString(),device=System.Guid.NewGuid().ToString();
            System.Action<long> lobby=epoch=>java.Lobby.Accept(new GorillaEscape.Contracts.LobbySnapshotWire {
                lobbyVersion=1,sessionId=session,phase="OPEN",players=new[]{new GorillaEscape.Contracts.LobbyPlayerWire {
                    playerId=1,deviceSessionId=device,connectionEpoch=epoch,connectionState="ACTIVE",networkReady=true,inputReady=true}}},null);
            System.Action<float,long,long> sample=(gamma,seq,epoch)=>java.PhoneInputs.Accept(new GorillaEscape.Contracts.PhoneInputWire {
                playerId=1,sessionId=session,deviceSessionId=device,sequence=seq,connectionEpoch=epoch,
                serverReceiveTimestamp=System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),messageType="MOTION_SAMPLE",connectionState="ACTIVE",
                quality=new GorillaEscape.Contracts.PhoneQuality{source="synthetic",status="available"},
                orientation=new GorillaEscape.Contracts.PhoneValues{hasGamma=true,gamma=gamma,availability="partial"},
                screenOrientation=new GorillaEscape.Contracts.PhoneValues{hasAngle=true,angle=0,availability="present"}});
            lobby(1);sample(10,1,1);menu.OpenLogBalance();var game=menu.LogBalance;
            game.SendMessage("OnApplicationFocus",true);
            Assert.False(game.BeginAttempt());
            for(int i=0;i<12;i++){sample(10,2+i,1);yield return new WaitForSecondsRealtime(.06f);}
            Assert.That(game.Attempt.State,Is.EqualTo(LogBalanceState.Countdown));
            game.Attempt.Tick(1,0);game.Attempt.Tick(1,0);game.Attempt.Tick(1,0);
            // Feed a short live stream while frames finish the countdown, then stop to test freshness loss.
            for(int frame=0;frame<8&&game.Attempt.FilteredTilt<=0;frame++){
                sample(35,14+frame,1);yield return new WaitForSecondsRealtime(.02f);
            }
            Assert.That(game.Attempt.FilteredTilt,Is.GreaterThan(0));
            yield return new WaitForSecondsRealtime(.45f);Assert.True(game.Paused);
            float distance=game.Attempt.Distance;yield return null;Assert.That(game.Attempt.Distance,Is.EqualTo(distance));
            lobby(2);sample(10,1,2);Assert.False(game.ResumeAttempt(),"New epoch requires calibration");
            for(int i=0;i<12;i++){sample(10,2+i,2);yield return new WaitForSecondsRealtime(.06f);}
            Assert.False(game.Paused);
            game.RestartAttempt();Assert.False(game.BeginAttempt(),"Each fresh attempt requires calibration");
            menu.CloseLogBalance();
        }
        finally {Object.Destroy(host);}
        yield return null;
    }
    [UnityTest] public IEnumerator LeavingPrototypeRestoresLightingAndShadowPreferences()
    {
        var mode=UnityEngine.RenderSettings.ambientMode;var fog=UnityEngine.RenderSettings.fog;
        var sky=UnityEngine.RenderSettings.ambientSkyColor;var sun=UnityEngine.RenderSettings.sun;
        var shadows=QualitySettings.shadows;var distance=QualitySettings.shadowDistance;
        var resolution=QualitySettings.shadowResolution;int cascades=QualitySettings.shadowCascades;
        var host=new GameObject("Log Balance lighting fixture");var menu=host.AddComponent<MainMenuController>();
        yield return null;
        try
        {
            menu.OpenLogBalance();
            Assert.That(UnityEngine.RenderSettings.ambientMode,Is.EqualTo(UnityEngine.Rendering.AmbientMode.Trilight));
            Assert.That(UnityEngine.RenderSettings.sun.shadows,Is.EqualTo(LightShadows.Soft));
            Assert.NotNull(GameObject.Find("Low-poly current highlight"));
            menu.CloseLogBalance();yield return null;
            Assert.That(UnityEngine.RenderSettings.ambientMode,Is.EqualTo(mode));Assert.That(UnityEngine.RenderSettings.fog,Is.EqualTo(fog));
            Assert.That(UnityEngine.RenderSettings.ambientSkyColor,Is.EqualTo(sky));Assert.AreSame(sun,UnityEngine.RenderSettings.sun);
            Assert.That(QualitySettings.shadows,Is.EqualTo(shadows));Assert.That(QualitySettings.shadowDistance,Is.EqualTo(distance));
            Assert.That(QualitySettings.shadowResolution,Is.EqualTo(resolution));Assert.That(QualitySettings.shadowCascades,Is.EqualTo(cascades));
        }
        finally{Object.Destroy(host);}
        yield return null;
    }
}
