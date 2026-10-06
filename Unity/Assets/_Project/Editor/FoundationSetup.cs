using GorillaEscape.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GorillaEscape.Editor
{
    public static class FoundationSetup
    {
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
            var settings = AssetDatabase.LoadAssetAtPath<ApplicationSettings>(settingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<ApplicationSettings>();
                AssetDatabase.CreateAsset(settings, settingsPath);
            }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var host = new GameObject("Bootstrap").AddComponent<Bootstrap>();
            var serialized = new SerializedObject(host);
            serialized.FindProperty("settings").objectReferenceValue = settings;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorBuildSettings.scenes = new[] {new EditorBuildSettingsScene(scenePath, true)};
            AssetDatabase.SaveAssets();
        }
    }
}
