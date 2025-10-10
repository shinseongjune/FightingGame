using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "FX/Effect Library")]
public sealed class EffectLibrary_SO : ScriptableObject
{
    public List<EffectEntry_SO> entries = new();
    private Dictionary<string, EffectEntry_SO> _map;

    public void BuildMap()
    {
        _map = new Dictionary<string, EffectEntry_SO>(entries.Count);
        foreach (var e in entries)
            if (e && !string.IsNullOrEmpty(e.key))
                _map[e.key] = e;
    }

    public bool TryGet(string key, out EffectEntry_SO entry)
    {
        if (_map == null) BuildMap();
        return _map.TryGetValue(key, out entry);
    }
}