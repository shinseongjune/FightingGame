using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterCatalog", menuName = "SO/Character Catalog")]
public class CharacterCatalog_SO : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string id;                 // 예: "TestMan"
        public string prefabKey;          // Addressables 키
        public List<string> clipKeys;     // 필요한 애니 키들(Idle/Walk/LP 등)
    }
    public List<Entry> entries = new();

    public bool TryGet(string id, out Entry e)
    {
        e = entries.Find(x => x.id == id);
        return e != null;
    }
}
