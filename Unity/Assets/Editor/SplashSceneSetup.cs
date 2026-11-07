using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Crea Assets/Scenes/Splash.unity con el SplashDirector y la iluminacion tropical,
/// y lo coloca como primera escena del build.
/// Invocable con: -executeMethod SplashSceneSetup.CreateSplashScene
/// </summary>
public static class SplashSceneSetup
{
    [MenuItem("Gorilla Escape/Create Splash Scene", priority = 5)]
    public static void CreateSplashScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var splash = new GameObject("SplashDirector");
        splash.AddComponent<SplashDirector>();

        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.25f;
        light.color = new Color(1f, 0.96f, 0.88f);
        lightGo.transform.rotation = Quaternion.Euler(46f, -32f, 0f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientLight = new Color(0.55f, 0.60f, 0.68f);
        RenderSettings.ambientSkyColor = new Color(0.55f, 0.75f, 0.95f);
        RenderSettings.ambientEquatorColor = new Color(0.52f, 0.58f, 0.58f);
        RenderSettings.ambientGroundColor = new Color(0.38f, 0.32f, 0.26f);

        string scenePath = "Assets/Scenes/Splash.unity";
        EditorSceneManager.SaveScene(scene, scenePath);

        var list = new System.Collections.Generic.List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(scenePath, true) };
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (s.path != scenePath)
                list.Add(s);
        }
        EditorBuildSettings.scenes = list.ToArray();

        Debug.Log("[SPLASH_SETUP] Scene saved and set as primary build scene: " + scenePath);
    }
}
