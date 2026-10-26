using GorillaEscape.Minigames.GorillaSmash;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GorillaEscape.Editor
{
    public static class GorillaSmashSetup
    {
        public const string ScenePath = "Assets/_Project/Scenes/Minigames/GorillaSmash.unity";
        public const string SettingsPath = "Assets/_Project/Resources/GorillaSmashSettings.asset";

        [MenuItem("Gorilla Escape/Gorilla Smash/Create Default Settings")]
        public static void CreateDefaultSettings()
        {
            if (AssetDatabase.LoadAssetAtPath<GorillaSmashSettings>(SettingsPath) == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources"))
                    AssetDatabase.CreateFolder("Assets/_Project", "Resources");

                var settings = ScriptableObject.CreateInstance<GorillaSmashSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
                AssetDatabase.SaveAssets();
                Debug.Log("GorillaSmashSettings asset created at " + SettingsPath);
            }
        }

        [MenuItem("Gorilla Escape/Gorilla Smash/Create Standalone Scene")]
        public static void CreateStandaloneScene()
        {
            CreateDefaultSettings();

            if (!AssetDatabase.IsValidFolder("Assets/_Project/Scenes/Minigames"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Scenes"))
                    AssetDatabase.CreateFolder("Assets/_Project", "Scenes");
                AssetDatabase.CreateFolder("Assets/_Project/Scenes", "Minigames");
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Add Directional Light for ambient jungle illumination
            var lightObj = new GameObject("Directional Light");
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1.0f, 0.95f, 0.88f);
            light.intensity = 1.1f;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Add GorillaSmashController GameObject
            var gameObj = new GameObject("GorillaSmashGame");
            gameObj.AddComponent<GorillaSmashController>();

            var saved = EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("Gorilla Smash scene saved: " + saved + " at " + ScenePath);
            AssetDatabase.Refresh();
        }

        [MenuItem("Gorilla Escape/Gorilla Smash/Open Standalone Scene")]
        public static void OpenStandaloneScene()
        {
            if (!File.Exists(ScenePath))
            {
                CreateStandaloneScene();
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(ScenePath);
            }
        }
    }
}
