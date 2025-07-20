using System;
using UnityEngine;

[Serializable]
public struct SkillInputData
{
    public InputData inputData;
    public bool isStrict;
    public int requiredChargeDuration;
    public int maxFrameGap;
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
}
