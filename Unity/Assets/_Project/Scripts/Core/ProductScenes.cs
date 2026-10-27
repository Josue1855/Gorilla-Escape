using System;
using System.Collections.Generic;

namespace GorillaEscape.Core
{
    public static class ProductScenes
    {
        public const string EntryScenePath = "Assets/Scenes/Connection.unity";
        public const string DiagnosticsScenePath = "Assets/_Project/Scenes/Bootstrap/Bootstrap.unity";
        static readonly string[] gameplay = {
            EntryScenePath, "Assets/Scenes/MainMenu.unity", "Assets/Scenes/BananaBowling.unity",
            "Assets/Scenes/GorillaBoxing.unity", "Assets/Scenes/JungleBaseball.unity", "Assets/Scenes/JungleDrums.unity",
            "Assets/Scenes/CoconutSlice.unity", "Assets/Scenes/WhackAMole.unity"
        };
        public static IReadOnlyList<string> GameplayScenes { get; } = Array.AsReadOnly(gameplay);
        public static bool IsGameplayScene(string path) => Array.IndexOf(gameplay, path) >= 0;
        public static string[] BuildScenePaths()
        {
            var paths = new string[gameplay.Length + 1];
            Array.Copy(gameplay, paths, gameplay.Length);
            paths[gameplay.Length] = DiagnosticsScenePath;
            return paths;
        }
        // Supplied by the Editor test runner; inherited environment flags alone cannot start hardware in tests.
        public static Func<bool> EditorPhysicalPermission { private get; set; }
        public static bool CanStartEditorServices(bool batch, string optIn)
            => !batch && optIn == "1" && EditorPhysicalPermission?.Invoke() == true;
    }
}
