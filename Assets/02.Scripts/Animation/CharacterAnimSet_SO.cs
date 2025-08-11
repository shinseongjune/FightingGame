using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AnimSet", menuName = "SO/Character Anim Set")]
public class CharacterAnimSet_SO : ScriptableObject
{
    [Serializable]
    public struct Entry { public AnimKey key; public string clipKey; }

    public List<Entry> entries = new();

    Dictionary<AnimKey, string> map;

    void OnEnable()
    {
        map = new Dictionary<AnimKey, string>(entries.Count);
        foreach (var e in entries) map[e.key] = e.clipKey;
    }

    void OnValidate()
    {
        if (entries == null) return;
        map = new Dictionary<AnimKey, string>(entries.Count);
        foreach (var e in entries) map[e.key] = e.clipKey; // 중복 키는 마지막 값이 우선
    }

    public bool TryGet(AnimKey key, out string clipKey)
    {
        if (map != null && map.TryGetValue(key, out clipKey))
            return true;

        clipKey = null;
        return false;
    }

    public string GetOrDefault(AnimKey key, string fallback = null)
    => TryGet(key, out var v) ? v : fallback;
}
