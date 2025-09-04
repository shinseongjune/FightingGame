using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct CharacterData
{
    public int id;
    public string addressableName;
    public string headerImgName;
    public string illustName;
    public string characterName;
    public Vector2 gridPos;
    public bool isLocked;
}

[Serializable]
public struct StageData
{
    public int id;
    public string addressableName;
    public string headerImgName;
    public string illustName;
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

    public void SetCharacterLocked(int id, bool locked)
    {
        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i].id == id)
            {
                characters[i].isLocked = locked;
                return;
            }
        }
    }

    public void SetStageLocked(int id, bool locked)
    {
        for (int i = 0; i < stages.Length; i++)
        {
            if (stages[i].id == id)
            {
                stages[i].isLocked = locked;
                return;
            }
        }
    }
}
