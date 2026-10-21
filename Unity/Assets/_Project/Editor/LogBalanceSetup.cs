using GorillaEscape.Minigames.LogBalance;
using UnityEditor;
using UnityEngine;
namespace GorillaEscape.Editor
{
    public static class LogBalanceSetup
    {
        [MenuItem("Gorilla Escape/Log Balance/Create Default Settings")]
        public static void CreateDefaultSettings()
        {
            const string path="Assets/_Project/Resources/LogBalanceSettings.asset";
            if(AssetDatabase.LoadAssetAtPath<LogBalanceSettings>(path)==null)
            {
                if(!AssetDatabase.IsValidFolder("Assets/_Project/Resources"))AssetDatabase.CreateFolder("Assets/_Project","Resources");
                var settings=ScriptableObject.CreateInstance<LogBalanceSettings>();
                AssetDatabase.CreateAsset(settings,path);AssetDatabase.SaveAssets();
            }
            const string materialPath="Assets/_Project/Resources/LogBalanceStage.mat";
            if(AssetDatabase.LoadAssetAtPath<Material>(materialPath)==null)
            {
                AssetDatabase.CreateAsset(new Material(Shader.Find("Standard")),materialPath);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
