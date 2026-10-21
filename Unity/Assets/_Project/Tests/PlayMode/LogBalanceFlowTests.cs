using System.Collections;
using GorillaEscape.Minigames.LogBalance;
using GorillaEscape.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class LogBalanceFlowTests
{
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
            for(int i=0;i<1500;i++)m.Tick(.02f,-.2f-m.Balance*.8f);
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
            Assert.True(game.Calibrate());Assert.True(game.BeginAttempt());game.Attempt.Tick(1,0);game.Attempt.Tick(1,0);game.Attempt.Tick(1,0);
            sample(35,2,1);yield return null;yield return null;
            Assert.That(game.Attempt.FilteredTilt,Is.GreaterThan(0));
            yield return new WaitForSecondsRealtime(.45f);Assert.True(game.Paused);
            float distance=game.Attempt.Distance;yield return null;Assert.That(game.Attempt.Distance,Is.EqualTo(distance));
            lobby(2);sample(10,1,2);Assert.False(game.ResumeAttempt(),"New epoch requires calibration");
            Assert.True(game.Calibrate());Assert.True(game.ResumeAttempt());
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
