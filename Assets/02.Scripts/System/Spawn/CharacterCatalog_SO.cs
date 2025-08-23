using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterCatalog", menuName = "SO/Character Catalog")]
public class CharacterCatalog_SO : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string id;                 // ��: "TestMan"
        public string prefabKey;          // Addressables Ű
        public List<string> clipKeys;     // �ʿ��� �ִ� Ű��(Idle/Walk/LP ��)
    }
    public List<Entry> entries = new();

    public bool TryGet(string id, out Entry e)
    {
        e = entries.Find(x => x.id == id);
        return e != null;
    }
}
