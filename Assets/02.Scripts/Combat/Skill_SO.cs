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
    [Tooltip("�� ������ 'Ŭ�� ������(clip.frameRate ����)'�Դϴ�. ��: Ŭ���� 20fps�� 1��=0..19")]
    public int startFrame;
    public int endFrame;

    public BoxType type;
    public BoxData box;

    [Tooltip("�� �ڽ��� ������ �� ���ο� ��Ʈ �ν��Ͻ��� ������� ����")]
    public bool incrementAttackInstance;
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

    [Header("�ڽ� ����")]
    public List<BoxLifetime> boxLifetimes = new();

    [Tooltip("������Ʈ ��ٿ� ������ ��")]
    public int rehitCooldownFrames;

    public string throwAnimationClipName;
    public string beingThrownAnimationClipName;

    //TODO: hit effect, �׳� ����Ʈ�� ���
    //����̺�
}
