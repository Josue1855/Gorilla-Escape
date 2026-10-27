using GorillaEscape.UI;
using GorillaEscape.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;

namespace GorillaEscape.Editor
{
    /// <summary>Editor entry and test isolation; never adds scenes, packages or physical adapters.</summary>
    [InitializeOnLoad]
    public static class ProductPlayMode
    {
        const string TestRunKey = "GorillaEscape.Product.TestRun";
        static readonly TestCallbacks Callbacks = new TestCallbacks();

        static ProductPlayMode()
        {
            GorillaEscape.QA.QaRuntimeService.EditorServicePermission = () => !SessionState.GetBool(TestRunKey, false);
            ProductScenes.EditorPhysicalPermission = () => !SessionState.GetBool(TestRunKey, false);
            MainMenuController.EditorLaunchPermission = () => !SessionState.GetBool(TestRunKey, false);
            TestRunnerApi.RegisterTestCallback(Callbacks);
            AssemblyReloadEvents.beforeAssemblyReload += () => TestRunnerApi.UnregisterTestCallback(Callbacks);
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
            if (!JungleEditorLaunch.Prepare()) return;
            var paths = ProductScenes.BuildScenePaths();
            var scenes = new EditorBuildSettingsScene[paths.Length];
            for (int i = 0; i < paths.Length; i++) scenes[i] = new EditorBuildSettingsScene(paths[i], true);
            EditorBuildSettings.scenes = scenes;
            EditorSceneManager.OpenScene(ProductScenes.EntryScenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Gorilla Escape/Open Bootstrap", true)]
        [MenuItem("Gorilla Escape/Play Gorilla Escape", true)]
        static bool CanOpen() => !EditorApplication.isPlayingOrWillChangePlaymode && !SessionState.GetBool(TestRunKey, false);

        sealed class TestCallbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor tests) => SessionState.SetBool(TestRunKey, true);
            public void RunFinished(ITestResultAdaptor result) => SessionState.SetBool(TestRunKey, false);
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
        }
    }
}
