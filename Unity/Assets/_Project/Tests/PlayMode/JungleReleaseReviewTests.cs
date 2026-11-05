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
        [UnityTearDown] public IEnumerator RestoreScene()
        { Time.timeScale=1; yield return SceneManager.LoadSceneAsync("Connection"); }
    }
}
