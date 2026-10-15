using System;
using System.IO;
using GorillaEscape.UI;
using GorillaEscape.QA;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Disposable isolated-editor harness. Not imported into the user's checkout or player.
[InitializeOnLoad]
public static class EditorProductSmoke
{
    const string Key="GorillaEscape.EditorSmoke.Stage";
    static string Output=>Environment.GetEnvironmentVariable("GORILLA_EDITOR_SMOKE_REPORT")??throw new InvalidOperationException("Explicit report path required");
    static double deadline;
    static EditorProductSmoke(){deadline=EditorApplication.timeSinceStartup+90;EditorApplication.update+=Tick;}
    public static void Start()
    {
        SessionState.SetInt(Key,1);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        EditorApplication.isPlaying=true;
    }
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static void Tick()
    {
        int stage=SessionState.GetInt(Key,0);if(stage==0)return;
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("EDITOR_SMOKE_TIMEOUT stage="+stage);
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(stage==1&&EditorApplication.isPlaying)
            {
                Require(SceneManager.GetActiveScene().path=="","expected Untitled");
                MainMenuController.TryLaunchForCurrentScene();
                Require(UnityEngine.Object.FindObjectsByType<MainMenuController>(FindObjectsSortMode.None).Length==0,"unexpected Untitled menu");
                SessionState.SetInt(Key,2);EditorApplication.isPlaying=false;
            }
            else if(stage==2&&!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                GorillaEscape.Editor.ProductPlayMode.PlayProduct();SessionState.SetInt(Key,3);
            }
            else if(stage==3&&EditorApplication.isPlaying)
            {
                if(Time.frameCount<15)return;
                var menu=UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
                Require(menu!=null,"Bootstrap auto-launch missing");
                if(menu.Runtime.JavaStatus==null)return;
                // Wait until runtime Start has initialized its adapters, without starting a backend.
                var f=typeof(QaRuntimeService).GetField("java",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                if(f.GetValue(menu.Runtime)==null)return;
                Require(((GorillaEscape.Network.JavaProbeSupervisor)f.GetValue(menu.Runtime)).Launches==0,"Java was launched");
                var vision=typeof(QaRuntimeService).GetField("vision",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                Require(((GorillaEscape.CameraTracking.VisionSupervisor)vision.GetValue(menu.Runtime)).Pid==0,"Vision was launched");
                MainMenuController.TryLaunchForCurrentScene();MainMenuController.TryLaunchForCurrentScene();
                Require(UnityEngine.Object.FindObjectsByType<MainMenuController>(FindObjectsSortMode.None).Length==1,"duplicate menu");
                Require(!menu.PlayEnabled,"JUGAR enabled");Require(MainMenuController.QaVisible,"PRUEBAS missing");
                Require(!menu.Runtime.ServicesEnabled,"Editor backend enabled");
                Require(menu.Runtime.JavaStatus.StartsWith("JAVA NOT RUNNING"),"Java status");
                Require(menu.Runtime.CameraStatus.StartsWith("CAMERA NOT RUNNING"),"Camera status");
                menu.OpenTests();Require(menu.InTests&&menu.Runtime.TestsActive,"PRUEBAS navigation");
                menu.Back();Require(!menu.InTests,"VOLVER navigation");
                Environment.SetEnvironmentVariable("GORILLA_QA_MENU","0");menu.OpenTests();Require(!menu.InTests,"hidden PRUEBAS accepted");Environment.SetEnvironmentVariable("GORILLA_QA_MENU",null);
                SessionState.SetInt(Key,4);menu.Exit();
            }
            else if(stage==4&&!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Require(UnityEngine.Object.FindObjectsByType<MainMenuController>(FindObjectsSortMode.None).Length==0,"SALIR residue");
                SessionState.SetInt(Key,5);GorillaEscape.Editor.ProductPlayMode.PlayProduct();
            }
            else if(stage==5&&EditorApplication.isPlaying)
            {
                if(Time.frameCount<15)return;
                var menu=UnityEngine.Object.FindFirstObjectByType<MainMenuController>();Require(menu!=null,"second Play missing");
                var f=typeof(QaRuntimeService).GetField("java",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);if(f.GetValue(menu.Runtime)==null)return;
                menu.OpenTests();Require(menu.InTests,"second PRUEBAS missing");
                SessionState.SetInt(Key,6);SessionState.SetInt(Key+".StopFrame",Time.frameCount+20);
            }
            else if(stage==6&&EditorApplication.isPlaying)
            {
                if(Time.frameCount<SessionState.GetInt(Key+".StopFrame",int.MaxValue))return;
                Require(UnityEngine.Object.FindFirstObjectByType<MainMenuController>().InTests,"QA route changed");
                SessionState.SetInt(Key,7);EditorApplication.isPlaying=false;
            }
            else if(stage==7&&!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Require(UnityEngine.Object.FindObjectsByType<MainMenuController>(FindObjectsSortMode.None).Length==0,"Stop residue");
                File.WriteAllText(Output,"{\"result\":\"PASS\",\"batchMode\":false,\"untitledNoMenu\":true,\"bootstrapAutoLaunch\":true,\"singleton\":true,\"playDisabled\":true,\"qaFlag\":true,\"navigation\":true,\"backendIdle\":true,\"exitStopsPlay\":true,\"stopPlayCleanup\":true,\"secondPlay\":true}");
                SessionState.SetInt(Key,0);EditorApplication.Exit(0);
            }
        }
        catch(Exception e){File.WriteAllText(Output,"{\"result\":\"FAIL\",\"stage\":"+stage+",\"reason\":\""+e.Message+"\"}");SessionState.SetInt(Key,0);Debug.LogException(e);EditorApplication.Exit(2);}
    }
}
