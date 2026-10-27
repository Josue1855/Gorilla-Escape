using System.Linq;
using GorillaEscape.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;

public class FoundationSceneTests
{
    [Test]
    public void SavedFoundationSceneHasValidSettingsAndIsIncludedInBuild()
    {
        const string path = "Assets/_Project/Scenes/Bootstrap/Bootstrap.unity";
        Assert.That(EditorBuildSettings.scenes.Any(s => s.enabled && s.path == path), Is.True);
        var settings = AssetDatabase.LoadAssetAtPath<ApplicationSettings>("Assets/_Project/Settings/ApplicationSettings.asset");
        Assert.That(settings, Is.Not.Null);
        Assert.That(settings.TargetFrameRate, Is.EqualTo(60));
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try
        {
            var roots = scene.GetRootGameObjects();
            var bootstraps = roots.SelectMany(r => r.GetComponentsInChildren<Bootstrap>(true)).ToArray();
            Assert.That(bootstraps, Has.Length.EqualTo(1));
            var serialized = new SerializedObject(bootstraps[0]);
            Assert.That(serialized.FindProperty("settings").objectReferenceValue, Is.EqualTo(settings));
            foreach (var go in roots.SelectMany(r => r.GetComponentsInChildren<UnityEngine.Transform>(true)))
                Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go.gameObject), Is.Zero);
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }
}
