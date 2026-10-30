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
    public const string JungleKeeper = "JungleKeeper";
    public const string LogBalance = "LogBalance";
    public const string WhackAMole = "WhackAMole";
    public static bool IsJungleKeeper { get; private set; }
    public static void Load(string scene)
    {
        if (string.IsNullOrEmpty(scene)) return;
        if (scene == JungleKeeper)
        {
            IsJungleKeeper = true;
            SceneManager.LoadScene(CoconutSlice);
            return;
        }
        IsJungleKeeper = false;
        SceneManager.LoadScene(scene);
    }
}
