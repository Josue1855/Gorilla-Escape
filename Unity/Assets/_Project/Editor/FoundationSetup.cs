using GorillaEscape.Core;
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GorillaEscape.Editor
{
    public static class FoundationSetup
    {
        public static void BuildDevelopmentLinux()
        {
            const string scenePath = "Assets/_Project/Scenes/Bootstrap/Bootstrap.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
                throw new InvalidOperationException("Generate and validate the foundation scene before building.");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            AssetDatabase.SaveAssets();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { scenePath },
                locationPathName = Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds/FoundationLinux/GorillaEscape.x86_64")),
                target = BuildTarget.StandaloneLinux64,
                options = BuildOptions.Development
            });
            Debug.Log($"Foundation Linux development build: {report.summary.result}; errors={report.summary.totalErrors}; warnings={report.summary.totalWarnings}");
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Foundation Linux development build failed.");
        }

        [MenuItem("Gorilla Escape/Create Foundation Scene") ]
        public static void CreateScene()
        {
            const string settingsPath = "Assets/_Project/Settings/ApplicationSettings.asset";
            const string scenePath = "Assets/_Project/Scenes/Bootstrap/Bootstrap.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null)
            {
                Debug.Log("Foundation scene already exists; leaving it unchanged.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var settings = AssetDatabase.LoadAssetAtPath<ApplicationSettings>(settingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<ApplicationSettings>();
                AssetDatabase.CreateAsset(settings, settingsPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(settingsPath, ImportAssetOptions.ForceSynchronousImport);
                settings = AssetDatabase.LoadAssetAtPath<ApplicationSettings>(settingsPath);
            }
            var host = new GameObject("Bootstrap").AddComponent<Bootstrap>();
            var serialized = new SerializedObject(host);
            serialized.Update();
            serialized.FindProperty("settings").objectReferenceValue = settings;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(host);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, scenePath))
                throw new InvalidOperationException("Could not save the foundation scene.");
            EditorBuildSettings.scenes = new[] {new EditorBuildSettingsScene(scenePath, true)};
            AssetDatabase.SaveAssets();
        }
    }
}
