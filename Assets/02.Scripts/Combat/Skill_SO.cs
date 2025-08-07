using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct SkillCondition
{
    public Skill_SO currentSkill;
    public CharacterStateTag currentCharacterState;
}

[Serializable]
public struct SkillInputData
{
    public InputData[] inputData;
    public bool isStrict;
    public int maxFrameGap;
}

[Serializable]
public struct BoxData
{
    public Vector2 center;
    public Vector2 size;
}

[Serializable]
public struct BoxLifetime
{
    public int startFrame;
    public int endFrame;

    public BoxData box;
}

[CreateAssetMenu(fileName = "New Skill", menuName = "SO/Skill")]
public class Skill_SO : ScriptableObject
{
    public string skillName;
    [TextArea] public string description;

    [Space(20)]

    public SkillCondition condition;

    [Space(20)]

    public SkillInputData command;

    [Space(20)]
    
    public int damageOnHit;
    public int hitstunDuration;
    public int blockstunDuration;

    public bool causesLaunch;
    public bool causesKnockdown;

    [Space(20)]
    public string animationClipName;

    [Header("박스 정보")]
    public List<BoxLifetime> boxLifetimes = new();
}
