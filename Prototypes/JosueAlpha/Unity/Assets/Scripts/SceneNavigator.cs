using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneNavigator
{
    public const string Connection = "Connection";
    public const string MainMenu = "MainMenu";
    public const string Bowling = "BananaBowling";
    public const string Boxing = "GorillaBoxing";
    public const string Baseball = "JungleBaseball";
    public const string Drums = "JungleDrums";
    public const string CoconutSlice = "CoconutSlice";
    public static void Load(string scene) { if (!string.IsNullOrEmpty(scene)) SceneManager.LoadScene(scene); }
}
