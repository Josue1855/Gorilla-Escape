using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Explicit, repeatable migration. AssetDatabase moves preserve GUIDs and scene refs.
public sealed class JunglePresentationMigration : IProcessSceneWithReport
{
    const string ReferencesRoot = "Assets/Resources/JungleAssetReferences";
    public int callbackOrder => 0;
    public void OnProcessScene(Scene scene, BuildReport report) => BakeBindings(scene);

    [MenuItem("Gorilla Escape/Migrate presentation references")]
    public static void MigrateAll()
    {
        MigrateAssets();
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
            {
                var scene = EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(guid), OpenSceneMode.Single);
                BakeBindings(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
        finally {
            if (setup.Length > 0 && setup.All(x => !string.IsNullOrEmpty(x.path) && File.Exists(x.path))) EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("JUNGLE_PRESENTATION_MIGRATION_COMPLETE");
    }
    static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        Folder(Path.GetDirectoryName(path).Replace('\\','/'));
        AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\','/'), Path.GetFileName(path));
    }
    public static void MigrateAssets()
    {
        Folder(ReferencesRoot); Folder("Assets/RuntimeContent");
        // Existing records own their stable keys, even when source art is renamed.
        var keysByPath = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string guid in AssetDatabase.FindAssets("t:JungleAssetReference", new[] { ReferencesRoot }))
        {
            string recordPath = AssetDatabase.GUIDToAssetPath(guid);
            string key = recordPath.Substring(ReferencesRoot.Length + 1);
            key = key.Substring(0, key.Length - ".asset".Length);
            foreach (var asset in AssetDatabase.LoadAssetAtPath<JungleAssetReference>(recordPath).Assets)
                if (asset != null) keysByPath[AssetDatabase.GetAssetPath(asset)] = key;
        }
        var sources = new List<(string path, string key)>();
        foreach (string guid in AssetDatabase.FindAssets("", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.StartsWith(ReferencesRoot, StringComparison.Ordinal) || AssetDatabase.IsValidFolder(path)) continue;
            int at = path.IndexOf("/Resources/", StringComparison.Ordinal);
            string key;
            if (!keysByPath.TryGetValue(path, out key))
            {
                if (at >= 0) key = path.Substring(at + "/Resources/".Length);
                else if (path.StartsWith("Assets/RuntimeContent/", StringComparison.Ordinal)) key = path.Substring("Assets/RuntimeContent/".Length);
                else continue;
                key = key.Substring(0, key.Length - Path.GetExtension(key).Length);
            }
            sources.Add((path, key));
        }
        foreach (var group in sources.GroupBy(x => x.key))
        {
            var assets = group.SelectMany(item => ReferencedAssets(item.path)).Where(x => x is GameObject || x is Texture || x is Font || x is Material || x is AnimationClip || x is TextAsset).Distinct().ToArray();
            if (assets.Length == 0) continue;
            string recordPath = ReferencesRoot + "/" + group.Key + ".asset";
            Folder(Path.GetDirectoryName(recordPath).Replace('\\','/'));
            var record = AssetDatabase.LoadAssetAtPath<JungleAssetReference>(recordPath);
            if (record == null) {
                record = ScriptableObject.CreateInstance<JungleAssetReference>();
                AssetDatabase.CreateAsset(record, recordPath);
            }
            record.SetAssets(assets); EditorUtility.SetDirty(record);
        }
        // Move whole source folders, including materials, models, textures and metas.
        foreach (string source in new[] { "Assets/Resources/Jungle", "Assets/_Project/Resources/Brand", "Assets/_Project/Resources/LogBalance" })
        {
            if (!AssetDatabase.IsValidFolder(source)) continue;
            string target = "Assets/RuntimeContent/" + Path.GetFileName(source);
            string error = AssetDatabase.MoveAsset(source, target);
            if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
        }
        // Any root resource material (e.g. LogBalanceStage) gets the same treatment.
        foreach (var item in sources.Where(x => x.path.Contains("/Resources/")))
        {
            if (AssetDatabase.LoadMainAssetAtPath(item.path) == null) continue;
            string target = "Assets/RuntimeContent/" + item.key + Path.GetExtension(item.path);
            Folder(Path.GetDirectoryName(target).Replace('\\','/'));
            string error = AssetDatabase.MoveAsset(item.path, target);
            if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
        }
        AssetDatabase.SaveAssets();
    }
    static IEnumerable<UnityEngine.Object> ReferencedAssets(string path)
    {
        // LoadAllAssetsAtPath also includes nested prefab GameObjects. Only the main
        // root is instantiable content; choosing a child loses the rig and hierarchy.
        var main = AssetDatabase.LoadMainAssetAtPath(path);
        return new[] { main }.Concat(AssetDatabase.LoadAllAssetsAtPath(path).Where(x => !(x is GameObject)));
    }
    public static void BakeBindings(Scene scene)
    {
        var objects = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).Select(x => x.gameObject).ToArray();
        foreach (var owner in objects)
            if (owner.GetComponents<MonoBehaviour>().Any(x => x is IJungleScreen) && owner.GetComponent<JunglePanel>() == null) owner.AddComponent<JunglePanel>();
        foreach (var owner in objects.Where(x => x.GetComponent<BowlingController>() != null))
        {
            var bindings = owner.GetComponent<JungleSceneBindings>();
            if (bindings == null) bindings = owner.AddComponent<JungleSceneBindings>();
            // Preserve stable keys for previously assigned objects, even after renaming.
            var old = bindings.Entries.Where(x => x.target != null).GroupBy(x => x.target).ToDictionary(x => x.Key, x => x.First().key);
            bindings.SetEntries(objects.Select(x => new JungleSceneBindings.Entry { key = old.TryGetValue(x, out var key) ? key : x.name, target = x }).ToArray());
            EditorUtility.SetDirty(bindings);
        }
    }
}
