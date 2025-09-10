using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "SO/StageCatalog")]
public class StageCatalog_SO : ScriptableObject
{
    public List<Entry> entries;
    [System.Serializable]
    public class Entry
    {
        public string stageName;
    }
}