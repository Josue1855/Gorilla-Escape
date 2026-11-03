using System;
using System.Collections.Generic;

namespace GorillaEscape.Core
{
    public static class ProductScenes
    {
        public const string SplashScenePath = "Assets/Scenes/Splash.unity";
        public const string ConnectionScenePath = "Assets/Scenes/Connection.unity";
        public const string EntryScenePath = SplashScenePath;
        public const string DiagnosticsScenePath = "Assets/_Project/Scenes/Bootstrap/Bootstrap.unity";
        static readonly string[] gameplay = {
            ConnectionScenePath, "Assets/Scenes/MainMenu.unity", "Assets/Scenes/BananaBowling.unity",
            "Assets/Scenes/GorillaBoxing.unity", "Assets/Scenes/JungleBaseball.unity", "Assets/Scenes/JungleDrums.unity",
            "Assets/Scenes/CoconutSlice.unity", "Assets/Scenes/WhackAMole.unity", "Assets/Scenes/LogBalance.unity",
            "Assets/Scenes/GorillaSmash.unity", "Assets/Scenes/CoconutThrow.unity"
        };
        public static IReadOnlyList<string> GameplayScenes { get; } = Array.AsReadOnly(gameplay);
        public static bool IsGameplayScene(string path) => Array.IndexOf(gameplay, path) >= 0;
        public static string[] BuildScenePaths()
        {
            var paths = new string[gameplay.Length + 2];
            paths[0] = SplashScenePath;
            Array.Copy(gameplay, 0, paths, 1, gameplay.Length);
            paths[gameplay.Length + 1] = DiagnosticsScenePath;
            return paths;
        }
        // Supplied by the Editor test runner; inherited environment flags alone cannot start hardware in tests.
        public static Func<bool> EditorPhysicalPermission { private get; set; }
        public static bool CanStartEditorServices(bool batch, string optIn)
            => !batch && optIn == "1" && EditorPhysicalPermission?.Invoke() == true;
    }
}
