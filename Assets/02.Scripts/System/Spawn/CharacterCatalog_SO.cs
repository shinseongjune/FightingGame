using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterCatalog", menuName = "SO/Character Catalog")]
public class CharacterCatalog_SO : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string characterName;                       // ¿¹: "TestMan"
        public int colorCount = 1;
        public CharacterAnimSet_SO animSet;
        public List<string> extraClipKeys;
    }
    public List<Entry> entries = new();
}
