using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Crea Assets/Scenes/Splash.unity con el SplashDirector y la iluminación, y lo coloca como
/// primera escena del build. Se invoca con: -executeMethod SplashSceneSetup.CreateSplashScene
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
        light.intensity = 1.15f;
        light.color = new Color(1f, 0.95f, 0.85f);
        lightGo.transform.rotation = Quaternion.Euler(48f, -28f, 0f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientLight = new Color(0.52f, 0.58f, 0.64f);
        RenderSettings.ambientSkyColor = new Color(0.55f, 0.72f, 0.9f);
        RenderSettings.ambientEquatorColor = new Color(0.5f, 0.55f, 0.55f);
        RenderSettings.ambientGroundColor = new Color(0.35f, 0.3f, 0.25f);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Splash.unity");

        var list = new System.Collections.Generic.List<EditorBuildSettingsScene> { new EditorBuildSettingsScene("Assets/Scenes/Splash.unity", true) };
        foreach (var s in EditorBuildSettings.scenes)
            if (s.path != "Assets/Scenes/Splash.unity") list.Add(s);
        EditorBuildSettings.scenes = list.ToArray();

        Debug.Log("SPLASH_SCENE_CREATED Assets/Scenes/Splash.unity");
    }
}
