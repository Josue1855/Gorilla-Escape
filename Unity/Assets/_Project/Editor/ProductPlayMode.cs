using GorillaEscape.QA;
using GorillaEscape.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace GorillaEscape.Editor
{
    /// <summary>Editor entry and test isolation; never adds scenes, packages or physical adapters.</summary>
    [InitializeOnLoad]
    public static class ProductPlayMode
    {
        const string TestRunKey = "GorillaEscape.Product.TestRun";
        const string LiveServicesKey = "GorillaEscape.LiveServicesInEditor";
        static readonly TestCallbacks Callbacks = new TestCallbacks();

        static ProductPlayMode()
        {
            QaRuntimeService.ForceServicesInEditor = EditorPrefs.GetBool(LiveServicesKey, true);
            QaRuntimeService.IsTestRunning = () => SessionState.GetBool(TestRunKey, false);
            MainMenuController.EditorLaunchPermission = () => !SessionState.GetBool(TestRunKey, false);
            TestRunnerApi.RegisterTestCallback(Callbacks);
            AssemblyReloadEvents.beforeAssemblyReload += () => TestRunnerApi.UnregisterTestCallback(Callbacks);
            EditorApplication.playModeStateChanged += change =>
            {
                if (change == PlayModeStateChange.EnteredPlayMode)
                {
                    SessionState.SetBool(TestRunKey, false);
                    QaRuntimeService.ForceServicesInEditor = EditorPrefs.GetBool(LiveServicesKey, true);
                }
                else if (change == PlayModeStateChange.ExitingPlayMode)
                {
                    var qa = UnityEngine.Object.FindFirstObjectByType<GorillaEscape.QA.QaRuntimeService>();
                    if (qa != null)
                    {
                        var task = qa.ShutdownAsync();
                        task.Wait(3000);
                    }
                }
            };
        }

        [MenuItem("Gorilla Escape/Live Services in Editor", priority = 2)]
        public static void ToggleLiveServices()
        {
            SessionState.SetBool(TestRunKey, false);
            bool next = !EditorPrefs.GetBool(LiveServicesKey, true);
            EditorPrefs.SetBool(LiveServicesKey, next);
            QaRuntimeService.ForceServicesInEditor = next;
            Debug.Log($"Gorilla Escape: Live Services in Editor set to {next}");
        }

        [MenuItem("Gorilla Escape/Live Services in Editor", true)]
        static bool ValidateLiveServices()
        {
            Menu.SetChecked("Gorilla Escape/Live Services in Editor", EditorPrefs.GetBool(LiveServicesKey, true));
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
            SessionState.SetBool(TestRunKey, false);
            EditorPrefs.SetBool(LiveServicesKey, true);
            QaRuntimeService.ForceServicesInEditor = true;
            EditorSceneManager.OpenScene(MainMenuController.BootstrapScenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

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
