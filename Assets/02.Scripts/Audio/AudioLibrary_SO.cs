using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "SFX/SFX Library")]
public sealed class AudioLibrary_SO : ScriptableObject
{
    public List<SfxEntry_SO> entries = new();
    private Dictionary<string, SfxEntry_SO> _map;

    public void BuildMap()
    {
        _map = new Dictionary<string, SfxEntry_SO>(entries.Count);
        foreach (var e in entries)
            if (e && !string.IsNullOrEmpty(e.key))
                _map[e.key] = e;
    }

    public bool TryGet(string key, out SfxEntry_SO entry)
    {
        if (_map == null || _map.Count != entries.Count) BuildMap();
        return _map.TryGetValue(key, out entry);
    }
}
