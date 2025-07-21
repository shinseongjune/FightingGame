using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct SkillInputData
{
    public InputData inputData;
    public bool isStrict;
    public int requiredChargeDuration;
    public int maxFrameGap;
}

[Serializable]
public struct BoxData
{
    public BoxType type;
    public Vector2 center;
    public Vector2 size;
    public int layer;
}

[Serializable]
public class SerializableFrameBoxMap
{
    public List<FrameBoxEntry> entries = new();

    [Serializable]
    public struct FrameBoxEntry
    {
        public int frame;
        public BoxData[] boxes;
    }

    public bool TryGetBoxes(int frame, out BoxData[] result)
    {
        foreach (var entry in entries)
        {
            if (entry.frame == frame)
            {
                result = entry.boxes;
                return true;
            }
        }
        result = null;
        return false;
    }
}

[CreateAssetMenu(fileName = "New Skill", menuName = "SO/Skill")]
public class Skill : ScriptableObject
{
    public string skillName;
    [TextArea] public string description;

    [Space(20)]

    public SkillInputData[] command;

    [Space(20)]
    
    public int damageOnHit;
    public int hitstunDuration;
    public int blockstunDuration;

    [Space(20)]
    public string animationClipName;

    public Skill[] nextSkills;

    [Header("박스 정보")]
    public SerializableFrameBoxMap frameToBoxes;
}
