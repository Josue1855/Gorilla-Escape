using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneNavigator
{
    public const string Splash = "Splash";
    public const string Connection = "Connection";
    public const string MainMenu = "MainMenu";
    public const string Bowling = "BananaBowling";
    public const string Boxing = "GorillaBoxing";
    public const string Baseball = "JungleBaseball";
    public const string Drums = "JungleDrums";
    public const string CoconutSlice = "CoconutSlice";
    public const string WhackAMole = "WhackAMole";
    public const string LogBalance = "LogBalance";
    public const string CoconutThrow = "CoconutThrow";
    public const string JungleKeeper = "JungleKeeper";
    public static bool IsMinigame(string scene) => scene is Bowling or Boxing or Baseball or Drums
        or CoconutSlice or WhackAMole or LogBalance or CoconutThrow or JungleKeeper;
    public static void Load(string scene) { if (scene == MainMenu && JungleStory.RequestExit()) return; if (!string.IsNullOrEmpty(scene)) SceneManager.LoadScene(scene); }
}
