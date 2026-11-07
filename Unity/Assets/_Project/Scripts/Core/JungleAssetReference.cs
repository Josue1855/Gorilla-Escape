using System;
using System.Collections.Generic;
using UnityEngine;

// Only lightweight reference records live in Resources; source art lives outside it.
// Each record loads independently, avoiding an eager catalog that retains every game.
public sealed class JungleAssetReference : ScriptableObject
{
    [SerializeField] UnityEngine.Object[] assets = Array.Empty<UnityEngine.Object>();
    public UnityEngine.Object[] Assets => assets;
#if UNITY_EDITOR
    public void SetAssets(UnityEngine.Object[] value) => assets = value;
#endif
}

public static class JungleAssets
{
    static readonly Dictionary<string, WeakReference<JungleAssetReference>> references = new Dictionary<string, WeakReference<JungleAssetReference>>(StringComparer.Ordinal);
    static readonly HashSet<string> missing = new HashSet<string>(StringComparer.Ordinal);
    static JungleAssetReference Resolve(string key)
    {
        if (references.TryGetValue(key, out var weak) && weak.TryGetTarget(out var cached) && cached != null) return cached;
        var record = Resources.Load<JungleAssetReference>("JungleAssetReferences/" + key);
        if (record != null) references[key] = new WeakReference<JungleAssetReference>(record);
        else if (missing.Add(key)) Debug.LogWarning("JUNGLE_ASSET_REFERENCE_MISSING: " + key);
        return record;
    }
    public static T Load<T>(string key) where T : UnityEngine.Object
    {
        var record = Resolve(key);
        if (record != null)
        {
            foreach (var asset in record.Assets)
                if (asset != null && asset is T typed) return typed;
        }
#if UNITY_EDITOR
        var directPath = "Assets/RuntimeContent/" + key;
        var directAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(directPath + ".prefab")
            ?? UnityEditor.AssetDatabase.LoadAssetAtPath<T>(directPath + ".fbx")
            ?? UnityEditor.AssetDatabase.LoadAssetAtPath<T>(directPath + ".png")
            ?? UnityEditor.AssetDatabase.LoadAssetAtPath<T>(directPath + ".mat")
            ?? UnityEditor.AssetDatabase.LoadAssetAtPath<T>(directPath + ".txt")
            ?? UnityEditor.AssetDatabase.LoadAssetAtPath<T>(directPath + ".asset")
            ?? UnityEditor.AssetDatabase.LoadAssetAtPath<T>(directPath);
        if (directAsset != null) return directAsset;
#endif
        var fallback = Resources.Load<T>(key);
        if (fallback != null) return fallback;
        return null;
    }
    public static T[] LoadAll<T>(string key) where T : UnityEngine.Object
    {
        var record = Resolve(key);
        var result = new List<T>();
        if (record != null)
        {
            foreach (var asset in record.Assets)
                if (asset != null && asset is T typed) result.Add(typed);
        }
        if (result.Count > 0) return result.ToArray();
#if UNITY_EDITOR
        var directPath = "Assets/RuntimeContent/" + key;
        foreach (var ext in new[] { ".prefab", ".fbx", ".asset" })
        {
            var sub = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(directPath + ext);
            if (sub != null && sub.Length > 0)
            {
                foreach (var s in sub) if (s != null && s is T typed) result.Add(typed);
                if (result.Count > 0) return result.ToArray();
            }
        }
#endif
        var fallbackAll = Resources.LoadAll<T>(key);
        if (fallbackAll != null && fallbackAll.Length > 0) return fallbackAll;
        return Array.Empty<T>();
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { references.Clear(); missing.Clear(); }
}
