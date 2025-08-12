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
    [Tooltip("이 값들은 '클립 프레임(clip.frameRate 기준)'입니다. 예: 클립이 20fps면 1초=0..19")]
    public int startFrame;
    public int endFrame;

    public BoxType type;
    public BoxData box;
}

public enum HitLevel { High, Mid, Low, Overhead }

[CreateAssetMenu(fileName = "New Skill", menuName = "SO/Skill")]
public class Skill_SO : ScriptableObject
{
    public string skillName;
    [TextArea] public string description;

    [Space(20)]

    public SkillCondition[] conditions;

    [Space(20)]

    public SkillInputData command;

    [Space(20)]
    
    public int damageOnHit;
    public int hitstunDuration;
    public int blockstunDuration;

    public bool causesLaunch;
    public bool causesKnockdown;

    public HitLevel hitLevel = HitLevel.High;

    [Space(20)]
    public string animationClipName;

    [Header("박스 정보")]
    public List<BoxLifetime> boxLifetimes = new();
}
