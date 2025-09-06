using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct CharacterData
{
    public string addressableName;
    public string characterName;
    public Vector2 gridPos;
    public bool isLocked;
}

[Serializable]
public struct StageData
{
    public string addressableName;
    public string stageName;
    public Vector2 gridPos;
    public bool isLocked;
}

public class SelectSceneModel : MonoBehaviour
{
    [Header("Master Data (View로 넘길 원본)")]
    [SerializeField] private CharacterData[] characters;
    [SerializeField] private StageData[] stages;

    public IReadOnlyList<CharacterData> Characters => characters;
    public IReadOnlyList<StageData> Stages => stages;
}
