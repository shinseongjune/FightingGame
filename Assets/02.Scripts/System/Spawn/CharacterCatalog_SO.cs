using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterCatalog", menuName = "SO/Character Catalog")]
public class CharacterCatalog_SO : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string id;                       // ��: "TestMan"
        public string prefabKey;                // Addressables Ű
        public CharacterAnimSet_SO animSet;
    }
    public List<Entry> entries = new();

    public string ResolvePrefabKey(string id)
    {
        var e = entries.Find(x => x.id == id);
        return e != null ? e.prefabKey : null;
    }
}
