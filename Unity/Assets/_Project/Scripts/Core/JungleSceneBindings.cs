using System;
using System.Collections.Generic;
using UnityEngine;

// Keys are authored once; renaming a referenced scene object preserves the binding.
public sealed class JungleSceneBindings : MonoBehaviour
{
    [Serializable] public struct Entry { public string key; public GameObject target; }
    [SerializeField] Entry[] entries = Array.Empty<Entry>();
    Dictionary<string, GameObject> lookup;
    public Entry[] Entries => entries;
    public GameObject EditableRoot => Get("ESCENARIO EDITABLE");
    public GameObject Get(string key)
    {
        if (lookup == null) {
            lookup = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            foreach (var entry in entries) if (!lookup.ContainsKey(entry.key)) lookup.Add(entry.key, entry.target);
        }
        return lookup.TryGetValue(key, out var target) ? target : null;
    }
#if UNITY_EDITOR
    public void SetEntries(Entry[] value) { entries = value; lookup = null; }
#endif
}
