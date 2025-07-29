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

    public HitRegion hitRegion;      // (고정값) 기본 상중하단
    public bool isAirAttack;         // 공중공격(동적 판정) 여부
    public InvincibleType invincibleType;
    public int superArmorCount;
    public int hitId;
}

[Serializable]
public struct BoxLifetime
{
    public int startFrame;
    public int endFrame; // inclusive

    public BoxData box;
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

    public bool causesLaunch;
    public bool causesKnockdown;

    [Space(20)]
    public string animationClipName;

    public Skill[] nextSkills;

    [Header("박스 정보")]
    public List<BoxLifetime> boxLifetimes = new();
}
