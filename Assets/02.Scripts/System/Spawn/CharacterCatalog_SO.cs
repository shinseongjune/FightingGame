using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterCatalog", menuName = "SO/Character Catalog")]
public class CharacterCatalog_SO : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string name;                       // ��: "TestMan"
        public int colorVariants = 1;
        public string prefabKey;                // Addressables Ű
        public CharacterAnimSet_SO animSet;
        public List<string> extraClipKeys;
    }
    public List<Entry> characters = new();

    public string ResolvePrefabKey(string id)
    {
        var e = characters.Find(x => x.name == id);
        return e != null ? e.prefabKey : null;
    }
}
