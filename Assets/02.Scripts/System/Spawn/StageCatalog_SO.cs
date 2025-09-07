using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Catalog/StageCatalog")]
public class StageCatalog_SO : ScriptableObject
{
    public List<Entry> entries;
    [System.Serializable]
    public class Entry
    {
        public string id;        // "Subway"
        public string stageKey;  // "scene:Stage/Subway" ¶Ç´Â "prefab:Stage/Subway"
    }

    public string ResolveKey(string id)
    {
        var e = entries.Find(x => x.id == id);
        return e != null ? e.stageKey : null;
    }
}