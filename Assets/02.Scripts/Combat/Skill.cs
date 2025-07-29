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

    public HitRegion hitRegion;      // (������) �⺻ �����ϴ�
    public bool isAirAttack;         // ���߰���(���� ����) ����
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

    [Header("�ڽ� ����")]
    public List<BoxLifetime> boxLifetimes = new();
}
