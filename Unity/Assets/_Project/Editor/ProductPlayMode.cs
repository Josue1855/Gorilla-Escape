using GorillaEscape.UI;
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;

namespace GorillaEscape.Editor
{
    /// <summary>Editor entry and test isolation; never adds scenes, packages or physical adapters.</summary>
    [InitializeOnLoad]
    public static class ProductPlayMode
    {
        static readonly string[] PhysicalVariables={"GORILLA_PACKAGE_ROOT","GORILLA_CONNECTION_MODE","GORILLA_CLOUDFLARED_PATH","GORILLA_IPC_JAVA","GORILLA_IPC_JAR","GORILLA_PHONE_INPUT","GORILLA_IPC_MODE","GORILLA_LOCAL_TRUST_DIR","GORILLA_TRUST_BOOTSTRAP_PORT","GORILLA_HTTPS_PORT","GORILLA_MOBILE_OPERATOR","GORILLA_EDITOR_PHYSICAL_QA","GORILLA_MAIN_MENU","GORILLA_QA_OVERLAY","GORILLA_QA_MENU","GORILLA_QA_AUTOSTART_TESTS"};
        const string PhysicalSession="GorillaEscape.Editor.PhysicalSession";
        const string TestRunKey = "GorillaEscape.Product.TestRun";
        static readonly TestCallbacks Callbacks = new TestCallbacks();

        static ProductPlayMode()
        {
            GorillaEscape.QA.QaRuntimeService.EditorServicePermission = PrepareHumanPlayServices;
            MainMenuController.EditorLaunchPermission = () => !SessionState.GetBool(TestRunKey, false);
            TestRunnerApi.RegisterTestCallback(Callbacks);
            EditorApplication.playModeStateChanged += state => {
                if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(PhysicalSession,false)){
                    foreach(string name in PhysicalVariables){string old=SessionState.GetString(PhysicalSession+name,"<unset>");Environment.SetEnvironmentVariable(name,old=="<unset>"?null:old);SessionState.EraseString(PhysicalSession+name);}
                    SessionState.SetBool(PhysicalSession,false);
                }
            };
            AssemblyReloadEvents.beforeAssemblyReload += () => TestRunnerApi.UnregisterTestCallback(Callbacks);
        }

        static bool PrepareHumanPlayServices()
        {
            if(Application.isBatchMode||SessionState.GetBool(TestRunKey,false)||!EditorApplication.isPlaying
                ||UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=MainMenuController.BootstrapScenePath)return false;
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../Builds/FoundationLinux"));
            foreach(string file in new[]{"Java/server.jar","Java/runtime/bin/java","Vision/python","Tools/cloudflared","tools/setup-firewall.sh"})
                if(!File.Exists(Path.Combine(root,file)))return false;
            if(!SessionState.GetBool(PhysicalSession,false))
            {
                foreach(string name in PhysicalVariables)SessionState.SetString(PhysicalSession+name,Environment.GetEnvironmentVariable(name)??"<unset>");
                SessionState.SetBool(PhysicalSession,true);
                Environment.SetEnvironmentVariable("GORILLA_PACKAGE_ROOT",root);
                GorillaEscape.QA.QaRuntimeService.ConfigurePackagedRuntime(root);
                Environment.SetEnvironmentVariable("GORILLA_EDITOR_PHYSICAL_QA","1");
            }
            return true;
        }

        [MenuItem("Gorilla Escape/Open Bootstrap", priority = 0)]
        public static void OpenBootstrap()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(MainMenuController.BootstrapScenePath, OpenSceneMode.Single);
        }

        [MenuItem("Gorilla Escape/Play Gorilla Escape", priority = 1)]
        public static void PlayProduct()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(MainMenuController.BootstrapScenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Gorilla Escape/Play Tests with Phone", priority = 2)]
        public static void PlayPhysicalTests()
        {
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../Builds/FoundationLinux"));
            foreach(string file in new[]{"Java/server.jar","Java/runtime/bin/java","Vision/python","Tools/cloudflared","tools/setup-firewall.sh"})
                if(!File.Exists(Path.Combine(root,file))){EditorUtility.DisplayDialog("Preparar pruebas","Faltan los recursos de conexión. Prepara el build local con ./run-game.sh --prepare-only antes de abrir las pruebas.","Aceptar");return;}
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            foreach(string name in PhysicalVariables)SessionState.SetString(PhysicalSession+name,Environment.GetEnvironmentVariable(name)??"<unset>");
            SessionState.SetBool(PhysicalSession,true);
            Environment.SetEnvironmentVariable("GORILLA_PACKAGE_ROOT",root);
            GorillaEscape.QA.QaRuntimeService.ConfigurePackagedRuntime(root);
            foreach(string name in new[]{"GORILLA_EDITOR_PHYSICAL_QA","GORILLA_MAIN_MENU","GORILLA_QA_OVERLAY","GORILLA_QA_MENU","GORILLA_QA_AUTOSTART_TESTS"})Environment.SetEnvironmentVariable(name,"1");
            EditorSceneManager.OpenScene(MainMenuController.BootstrapScenePath,OpenSceneMode.Single);
            EditorApplication.isPlaying=true;
        }

        [MenuItem("Gorilla Escape/Play Tests with Phone", true)]
        [MenuItem("Gorilla Escape/Open Bootstrap", true)]
        [MenuItem("Gorilla Escape/Play Gorilla Escape", true)]
        static bool CanOpen() => !EditorApplication.isPlayingOrWillChangePlaymode;

        sealed class TestCallbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor tests) => SessionState.SetBool(TestRunKey, true);
            public void RunFinished(ITestResultAdaptor result) => SessionState.SetBool(TestRunKey, false);
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
        }
    }
}
