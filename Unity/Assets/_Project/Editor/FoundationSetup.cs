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

        [Serializable] public sealed class SourceFile { public string path,sha256; }
        [Serializable] public sealed class GameBuildManifest { public int schema=1;public string unityVersion;public string[] scenes;public SourceFile[] sources; }
        [MenuItem("Gorilla Escape/Build Seven Games Linux")]
        public static void BuildDevelopmentGameLinux()
        {
            var scenes=ProductScenes.BuildScenePaths();
            foreach(var scene in scenes)if(AssetDatabase.LoadAssetAtPath<SceneAsset>(scene)==null)throw new InvalidOperationException("Missing game scene: "+scene);
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
            string output=Path.Combine(root,"Unity/Builds/JungleLinux/GorillaEscape.x86_64");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            string marker=Path.Combine(Path.GetDirectoryName(output),"game-build.json");
            if(File.Exists(marker))File.Delete(marker);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            AssetDatabase.SaveAssets();
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes,locationPathName=output,target=BuildTarget.StandaloneLinux64,options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Seven-game build failed; no launch marker written.");
            var paths=new System.Collections.Generic.List<string>();
            foreach(string folder in new[]{"Unity/Assets","Shared/CSharp/Runtime"})
                foreach(string path in Directory.GetFiles(Path.Combine(root,folder),"*",SearchOption.AllDirectories))
                    if(path.EndsWith(".cs")||path.EndsWith(".unity")||path.EndsWith(".asmdef"))paths.Add(path);
            foreach(string path in new[]{"Unity/Packages/manifest.json","Unity/Packages/packages-lock.json","Unity/ProjectSettings/EditorBuildSettings.asset","Unity/ProjectSettings/ProjectVersion.txt"})paths.Add(Path.Combine(root,path));
            paths.Sort(StringComparer.Ordinal);var records=new System.Collections.Generic.List<SourceFile>();
            foreach(string path in paths){using(var file=File.OpenRead(path))using(var sha=System.Security.Cryptography.SHA256.Create())records.Add(new SourceFile{path=path.Substring(root.Length+1).Replace('\\','/'),sha256=BitConverter.ToString(sha.ComputeHash(file)).Replace("-","").ToLowerInvariant()});}
            File.WriteAllText(marker,JsonUtility.ToJson(new GameBuildManifest{unityVersion=Application.unityVersion,scenes=scenes,sources=records.ToArray()},true));
            Debug.Log("Seven-game Linux build prepared: "+output);
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
