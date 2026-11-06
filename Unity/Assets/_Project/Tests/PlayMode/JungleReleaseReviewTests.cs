using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace GorillaEscape.Tests
{
    // Presentation fixtures deliberately force phases. They do not certify physical input.
    public sealed class JungleReleaseReviewTests
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly string[][] Games = {
            new[]{"GorillaSmash","GorillaSmashController","Intro","Playing","Results"},
            new[]{"CoconutThrow","CoconutThrowController","Intro","Aiming","Results"},
            new[]{"BananaBowling","BowlingController","Connecting","Aim","Results"},
            new[]{"CoconutSlice","CoconutSliceController","Intro","Playing","Results"},
            new[]{"LogBalance","LogBalanceGameController","Ready","Crossing","Won"},
            new[]{"GorillaBoxing","BoxingController","Boxing","BoxingKnockdown","BoxingOver"},
            new[]{"JungleBaseball","BaseballController","Ready","Pitching","GameOver"},
            new[]{"JungleDrums","RhythmDrumsController","ready","playing","finished"},
            new[]{"WhackAMole","WhackAMoleController","Intro","Playing","Results"},
            new[]{"JungleKeeper","CoconutSliceController","Intro","Playing","Results"}
        };
        static void Set(object target, string field, object value)
        { target.GetType().GetField(field,Flags)?.SetValue(target,value); }
        static void Phase(MonoBehaviour controller, string phase, bool last)
        {
            var state=controller.GetType().GetField("state",Flags);
            if(state!=null && state.FieldType.IsEnum) {
                if(!Enum.IsDefined(state.FieldType,phase)) throw new Exception(controller.GetType().Name+" unknown phase "+phase);
                state.SetValue(controller,Enum.Parse(state.FieldType,phase));
            }
            Set(controller,"finished",last);
            Set(controller,"bowlingGameOver",last);
            Set(controller,"endAt",Time.time+35f); Set(controller,"roundEndsAt",Time.time+35f);
            Set(controller,"score",120); Set(controller,"hits",3);
            if(controller.GetType().Name=="LogBalanceGameController") {
                var model=controller.GetType().GetField("model",Flags).GetValue(controller);
                var backing=model.GetType().GetField("<State>k__BackingField",Flags);
                backing.SetValue(model,Enum.Parse(backing.FieldType,phase));
                Set(controller,"preparing",false);
                if(last){
                    var resultType=model.GetType().Assembly.GetType("GorillaEscape.Minigames.LogBalance.LogBalanceResult");
                    var result=Activator.CreateInstance(resultType,Flags,null,new object[]{1,true,30f,18f,1f,"COMPLETADO"},null);
                    model.GetType().GetField("<Result>k__BackingField",Flags).SetValue(model,result);
                    model.GetType().GetField("<Distance>k__BackingField",Flags).SetValue(model,18f);
                }
            }
        }
        [UnityTest, Timeout(300000)]
        public IEnumerator AllGamesHaveUsablePhasesMaterialsAndMenuButtons()
        {
            var navigation=Type.GetType("SceneNavigator, Assembly-CSharp",true);
            var output=Environment.GetEnvironmentVariable("GORILLA_RELEASE_REVIEW_DIR");
            var failures=new List<string>();
            foreach(var game in Games) {
                navigation.GetMethod("Load").Invoke(null,new object[]{game[0]});
                for(int i=0;i<16;i++)yield return null;
                var scene=SceneManager.GetActiveScene();
                foreach(var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                    if(behaviour.GetType().Name=="MotionSession")behaviour.enabled=false;
                Time.timeScale=0;
                var controller=Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).First(x=>x.GetType().Name==game[1]);
                var document=controller.GetComponent<UIDocument>();
                var renderers=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.gameObject.scene==scene&&r.enabled).ToArray();
                if(renderers.Length==0)failures.Add(game[0]+": no 3D renderers");
                foreach(var renderer in renderers) foreach(var material in renderer.sharedMaterials)
                    if(material==null||material.shader==null||!material.shader.isSupported||material.shader.name.Contains("InternalError"))
                        failures.Add(game[0]+": missing/unsupported material on "+renderer.name);
                for(int phase=0;phase<3;phase++) {
                    string label=game[phase+2];
                    try{Phase(controller,label,phase==2);}catch(Exception e){failures.Add(e.Message);continue;}
                    for(int i=0;i<4;i++)yield return null;
                    if(label=="Intro")yield return new WaitForSecondsRealtime(.9f);
#if UNITY_EDITOR
                    while(UnityEditor.ShaderUtil.anythingCompiling)yield return null;
#endif
                    var buttons=document.rootVisualElement.Query<Button>().ToList().Where(b=>b.resolvedStyle.display!=DisplayStyle.None).ToArray();
                    foreach(var button in buttons) {
                        var rect=button.worldBound;
                        if(rect.xMin < -2||rect.yMin < -2||rect.xMax>Screen.width+2||rect.yMax>Screen.height+2)
                            failures.Add(game[0]+"/"+label+": offscreen button "+button.text+" "+rect);
                    }
                    if(!string.IsNullOrEmpty(output)) {
                        Directory.CreateDirectory(output);
                        File.AppendAllText(Path.Combine(output,"inventory.txt"),game[0]+"/"+label+" renderers="+renderers.Length+" buttons="+string.Join(" | ",buttons.Select(b=>b.text))+"\n");
                        yield return new WaitForEndOfFrame();
                        var shot=ScreenCapture.CaptureScreenshotAsTexture();
                        File.WriteAllBytes(Path.Combine(output,game[0]+"-"+label+".png"),shot.EncodeToPNG());Object.Destroy(shot);
                    }
                }
                var retry=document.rootVisualElement.Query<Button>().ToList().FirstOrDefault(b=>b.resolvedStyle.display!=DisplayStyle.None&&new[]{"REINTENTAR","OTRA PARTIDA","OTRA RONDA","OTRO INTENTO","REVANCHA","REPETIR"}.Contains(b.text));
                if(retry==null)failures.Add(game[0]+": no replay button in results");
                else {
                    using(var click=NavigationSubmitEvent.GetPooled()){click.target=retry;retry.SendEvent(click);}
                    for(int i=0;i<8;i++)yield return null;
                    controller=Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).First(x=>x.GetType().Name==game[1]);
                    document=controller.GetComponent<UIDocument>();
                    foreach(var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))if(behaviour.GetType().Name=="MotionSession")behaviour.enabled=false;
                    var state=controller.GetType().GetField("state",Flags);
                    if(state!=null&&state.GetValue(controller).ToString()==game[4])failures.Add(game[0]+": replay did not reset the finished phase");
                    if(controller.GetType().Name=="RhythmDrumsController"&&(bool)controller.GetType().GetField("finished",Flags).GetValue(controller))failures.Add(game[0]+": replay stayed finished");
                    if(!string.IsNullOrEmpty(output))File.AppendAllText(Path.Combine(output,"inventory.txt"),game[0]+" replay=activated\n");
                }
                var menu=document.rootVisualElement.Query<Button>().ToList().FirstOrDefault(b=>b.resolvedStyle.display!=DisplayStyle.None&&b.text.Contains("MENÚ"));
                if(menu==null)failures.Add(game[0]+": no exit button in results");
                else {
                    using(var click=NavigationSubmitEvent.GetPooled()){click.target=menu;menu.SendEvent(click);}
                    for(int i=0;i<8;i++)yield return null;
                    if(SceneManager.GetActiveScene().name!="MainMenu")failures.Add(game[0]+": menu button did not navigate");
                }
                Time.timeScale=1;
            }
            if(!string.IsNullOrEmpty(output))File.WriteAllLines(Path.Combine(output,"findings.txt"),failures);
            Assert.That(failures,Is.Empty,string.Join("\n",failures));
        }
        [UnityTest]
        public IEnumerator FinishedRoundReleasesTechnicalPauseAndKeepsResultsAccessible()
        {
            yield return SceneManager.LoadSceneAsync("JungleDrums");yield return null;
            var session=Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).First(x=>x.GetType().Name=="MotionSession");
            var tracking=session.GetType().GetField("trackingPause",Flags).GetValue(session);
            var update=tracking.GetType().GetMethod("Update");
            update.Invoke(tracking,new object[]{false,0f,.4f});update.Invoke(tracking,new object[]{false,2f,.4f});
            var lease=session.GetType().GetField("pause",Flags).GetValue(session);
            Time.timeScale=(float)lease.GetType().GetMethod("Apply").Invoke(lease,new object[]{true,1f});
            session.GetType().GetMethod("PresentGame").Invoke(session,new object[]{"Finished",null,null,null,null,null});
            session.GetType().GetMethod("RefreshPause",Flags).Invoke(session,null);
            Assert.That(tracking.GetType().GetMethod("ShowNotice").Invoke(tracking,new object[]{Time.unscaledTime}),Is.EqualTo(false));
            Assert.That(((IJungleModalScreen)session).BlocksSceneInput,Is.False);
            Assert.That(Time.timeScale,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator MainMenuCameraPreviewStartsCollapsedBelowModeButtons()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");for(int i=0;i<8;i++)yield return null;
            var session=Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).First(x=>x.GetType().Name=="MotionSession");
            Assert.That(session.GetType().GetField("pipMinimized",Flags).GetValue(session),Is.EqualTo(true));
            var toggle=session.GetComponent<UIDocument>().rootVisualElement.Query<Button>().ToList().FirstOrDefault(b=>b.text=="▲");
            Assert.That(toggle,Is.Not.Null);
            Assert.That(toggle.worldBound.yMin,Is.GreaterThan(Screen.height*.88f));
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator AllTenMinigamesExecuteRealGameplayAndFourPlayerTournamentTurns()
        {
            Time.timeScale = 1f;
            var navigation = Type.GetType("SceneNavigator, Assembly-CSharp", true);
            var tournamentType = Type.GetType("TournamentSession, Assembly-CSharp", true);
            var gameModeType = tournamentType.GetNestedType("GameMode", BindingFlags.Public);
            var partyMode = Enum.Parse(gameModeType, "Party");
            tournamentType.GetMethod("StartMode", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { partyMode, 4, "Náufrago 1" });

            var output = Environment.GetEnvironmentVariable("GORILLA_RELEASE_REVIEW_DIR");
            var reportLines = new List<string>();
            int roundIdx = 0;

            foreach (var game in Games)
            {
                string gameId = game[0];
                string controllerName = game[1];
                navigation.GetMethod("Load").Invoke(null, new object[] { gameId });
                for (int i = 0; i < 12; i++) yield return null;

                Time.timeScale = 1f;
                var controller = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).First(x => x.GetType().Name == controllerName);
                Assert.That(controller, Is.Not.Null, gameId + " controller missing");

                var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => r.enabled).ToArray();
                var rigidbodies = Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).ToArray();
                var audioSources = Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).ToArray();
                Assert.That(renderers.Length, Is.GreaterThan(0), gameId + " must have active 3D renderers");
                Assert.That(audioSources.Length, Is.GreaterThan(0), gameId + " must have active AudioSource");

                // Let real Update/FixedUpdate frames execute at timeScale=1
                Phase(controller, game[3], false);
                for (int frame = 0; frame < 15; frame++) yield return null;

                // Verify 4-player turn rotation in TournamentSession for this round
                int[] turnScores = { 120 + roundIdx * 5, 95 + roundIdx * 3, 140 + roundIdx * 4, 110 + roundIdx * 2 };
                for (int turn = 0; turn < 4; turn++)
                {
                    int expectedPlayerId = turn + 1;
                    int activeTurnPlayer = (int)tournamentType.GetProperty("CurrentTurnPlayerId", BindingFlags.Static | BindingFlags.Public).GetValue(null);
                    Assert.That(activeTurnPlayer, Is.EqualTo(expectedPlayerId), $"{gameId} turn {turn} expected player {expectedPlayerId}");

                    tournamentType.GetMethod("RecordRoundResult", BindingFlags.Static | BindingFlags.Public).Invoke(null, new object[] { turnScores[turn] });
                    // Calling RecordRoundResult again on same turn must be idempotent (DEF-04 regression check)
                    float turnStartedFirst = (float)tournamentType.GetProperty("TurnStartedAt", BindingFlags.Static | BindingFlags.Public).GetValue(null);
                    yield return null;
                    tournamentType.GetMethod("RecordRoundResult", BindingFlags.Static | BindingFlags.Public).Invoke(null, new object[] { turnScores[turn] });
                    float turnStartedSecond = (float)tournamentType.GetProperty("TurnStartedAt", BindingFlags.Static | BindingFlags.Public).GetValue(null);
                    Assert.That(turnStartedSecond, Is.EqualTo(turnStartedFirst), $"{gameId} turn {turn} RecordRoundResult must not reset TurnStartedAt on subsequent frames");

                    if (turn < 3)
                    {
                        bool isTransition = (bool)tournamentType.GetProperty("IsTurnTransition", BindingFlags.Static | BindingFlags.Public).GetValue(null);
                        Assert.That(isTransition, Is.True, $"{gameId} turn {turn} should enter IsTurnTransition");
                        bool hasNext = (bool)tournamentType.GetMethod("AdvancePlayerTurn", BindingFlags.Static | BindingFlags.Public).Invoke(null, new object[] { false });
                        Assert.That(hasNext, Is.True, $"{gameId} turn {turn} should advance to next player");
                        yield return null;
                    }
                }

                bool roundCompleted = (bool)tournamentType.GetProperty("RoundCompleted", BindingFlags.Static | BindingFlags.Public).GetValue(null);
                Assert.That(roundCompleted, Is.True, $"{gameId} after 4th player turn should mark RoundCompleted=true");

                reportLines.Add($"{{\"game\":\"{gameId}\",\"controller\":\"{controllerName}\",\"renderers\":{renderers.Length},\"rigidbodies\":{rigidbodies.Length},\"audioSources\":{audioSources.Length},\"turnsCompleted\":4,\"roundCompleted\":true,\"status\":\"PASS\"}}");

                if (roundIdx < 9)
                {
                    tournamentType.GetMethod("NextRound", BindingFlags.Static | BindingFlags.Public).Invoke(null, null);
                    yield return null;
                }
                roundIdx++;
            }

            tournamentType.GetMethod("ResetTournament", BindingFlags.Static | BindingFlags.Public).Invoke(null, null);
            if (!string.IsNullOrEmpty(output))
            {
                Directory.CreateDirectory(output);
                File.WriteAllText(Path.Combine(output, "real_gameplay_10_minigames_playmode.json"), "[\n  " + string.Join(",\n  ", reportLines) + "\n]\n");
            }
        }
        [UnityTearDown] public IEnumerator RestoreScene()
        { Time.timeScale=1; yield return SceneManager.LoadSceneAsync("Connection"); }
    }
}
