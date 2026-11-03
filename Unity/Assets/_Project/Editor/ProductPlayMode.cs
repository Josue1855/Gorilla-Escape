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

        [MenuItem("Gorilla Escape/Build Windows Standalone", priority = 2)]
        public static void BuildWindowsStandalone()
        {
            var paths = ProductScenes.BuildScenePaths();
            string buildPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath, "../../Builds/Windows/GorillaEscape.exe"));
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(buildPath));
            UnityEngine.Debug.Log($"[BUILD_WINDOWS] Starting build of {paths.Length} scenes to {buildPath}...");
            var report = BuildPipeline.BuildPlayer(paths, buildPath, BuildTarget.StandaloneWindows64, BuildOptions.None);
            UnityEngine.Debug.Log($"[BUILD_WINDOWS] Result={report.summary.result} Errors={report.summary.totalErrors} Path={buildPath}");
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                UnityEngine.Debug.LogError($"[BUILD_WINDOWS_FAILED] Total errors: {report.summary.totalErrors}");
                if (UnityEditorInternal.InternalEditorUtility.inBatchMode)
                    EditorApplication.Exit(1);
            }
            else
            {
                UnityEngine.Debug.Log($"[BUILD_WINDOWS_SUCCESS] Size={report.summary.totalSize} bytes, Time={report.summary.totalTime.TotalSeconds:F1}s");
                if (UnityEditorInternal.InternalEditorUtility.inBatchMode)
                    EditorApplication.Exit(0);
            }
        }

        [MenuItem("Gorilla Escape/Open Bootstrap", true)]
        [MenuItem("Gorilla Escape/Play Gorilla Escape", true)]
        [MenuItem("Gorilla Escape/Build Windows Standalone", true)]
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
