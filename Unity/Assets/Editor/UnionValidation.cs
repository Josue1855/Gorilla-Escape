
using System;
using GorillaEscape.Minigames.LogBalance;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class UnionValidation
{
    public static void Validate()
    {
        CheckScene<ConnectionScreen>(SceneNavigator.Connection);
        CheckScene<MainMenuController>(SceneNavigator.MainMenu);
        CheckScene<WhackAMoleController>(SceneNavigator.WhackAMole);
        CheckScene<CoconutSliceController>(SceneNavigator.CoconutSlice);
        CheckScene<CoconutThrowController>(SceneNavigator.CoconutThrow);
        CheckScene<JungleKeeperController>(SceneNavigator.JungleKeeper);
        CheckScene<LogBalanceGameController>(SceneNavigator.LogBalance);

        var probe = new GameObject("Log Balance validation");
        var stage = new LogBalanceStage(probe.transform, new LogBalanceRules().length);
        if (stage.Character == null) throw new InvalidOperationException("Log Balance character missing.");
        stage.Dispose();
        UnityEngine.Object.DestroyImmediate(probe);

        Require<Font>("Brand/LilitaOne");
        Require<Font>("Brand/NunitoSans");
        const string root = "Jungle/NewModels/WhackAMole/";
        foreach (string name in new[] {
            "Meshy_AI_model_20261026_texture", "Meshy_AI_Cheeky_Monkey_20261026_texture",
            "Meshy_AI_Dizzy_Monkey_20261026_texture", "Meshy_AI_Wooden_War_Drum_20261026_texture" })
            Require<GameObject>(root + name + "_fbx/" + name);
        Debug.Log("UNION_VALIDATION_PASS");
    }

    private static void CheckScene<T>(string name) where T : MonoBehaviour
    {
        string path = "Assets/Scenes/" + name + ".unity";
        if (Array.FindIndex(EditorBuildSettings.scenes, scene => scene.enabled && scene.path == path) < 0)
            throw new InvalidOperationException("Missing build scene " + name);
        EditorSceneManager.OpenScene(path);
        if (UnityEngine.Object.FindAnyObjectByType<T>() == null)
            throw new InvalidOperationException("Missing controller in " + name);
    }

    private static void Require<T>(string path) where T : UnityEngine.Object
    {
        if (Resources.Load<T>(path) == null) throw new InvalidOperationException("Missing resource " + path);
    }
}

