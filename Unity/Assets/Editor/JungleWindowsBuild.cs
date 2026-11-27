using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class JungleWindowsBuild
{
    public static void Build()
    {
        PlayerSettings.productName = "Jungle Party";
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.defaultIsNativeResolution = true;
        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Application/JunglePartyIcon.png");
        if (icon == null) throw new InvalidOperationException("Falta el icono de Jungle Party.");
        int[] sizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Unknown);
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, sizes.Select(size => icon).ToArray());
        sizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Standalone);
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone, sizes.Select(size => icon).ToArray());
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string output = Path.Combine(root, "Builds", "JunglePartyBeta", "JungleParty.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        if (scenes.Length == 0) throw new InvalidOperationException("No hay escenas habilitadas.");
        var graphics = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
        SerializedProperty included = graphics.FindProperty("m_AlwaysIncludedShaders");
        var original = Enumerable.Range(0, included.arraySize)
            .Select(index => included.GetArrayElementAtIndex(index).objectReferenceValue).ToArray();
        try
        {
            // These materials are created by name at runtime, so scene scanning cannot retain their shaders.
            foreach (string name in new[] { "Unlit/Texture", "Unlit/Color", "Unlit/Transparent",
                "Sprites/Default", "Mobile/Diffuse", "Mobile/Unlit (Supports Lightmap)" })
            {
                Shader shader = Shader.Find(name);
                if (shader == null || original.Contains(shader)) continue;
                int index = included.arraySize++;
                included.GetArrayElementAtIndex(index).objectReferenceValue = shader;
            }
            graphics.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Build Windows: " + report.summary.result);
            Debug.Log("JUNGLE_WINDOWS_BUILD_OK " + output);
        }
        finally
        {
            graphics.Update();
            included = graphics.FindProperty("m_AlwaysIncludedShaders");
            included.arraySize = original.Length;
            for (int i = 0; i < original.Length; i++)
                included.GetArrayElementAtIndex(i).objectReferenceValue = original[i];
            graphics.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }
    }
}

