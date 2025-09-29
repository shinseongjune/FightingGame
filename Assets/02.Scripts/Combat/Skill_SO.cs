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

    [Tooltip("이 박스가 생성될 때 새로운 히트 인스턴스로 취급할지 여부")]
    public bool incrementAttackInstance;
}

public enum HitLevel { High, Mid, Low, Overhead }
public enum SkillFlag { None, DriveImpact, DriveParry, DriveRush, DriveReversal }

[System.Serializable]
public class PushProfile
{
    public float hitDistance = 0.45f;
    public int hitDurationFrames = 8;
    public float guardDistance = 0.30f;
    public int guardDurationFrames = 6;
    public float airHitDistance = 0.35f;
    public int airHitDurationFrames = 6;
    public int easingType = 0; // 0=선형, 1/2/3=ease
}

public enum KnockdownMode
{
    None,   // 넉다운 아님
    Trip,   // 바닥으로 그냥 넘김(수직 변화 X)
    PopUp,  // 살짝 띄워서 넘김(짧은 리프트만)
}

[System.Serializable]
public class KnockdownProfile
{
    public KnockdownMode mode = KnockdownMode.None;

    [Tooltip("PopUp일 때 정점 높이(미터). 아주 작게(예: 0.4~0.7)")]
    public float popUpApexHeight = 0.5f;

    [Tooltip("넘어진 뒤 강제 다운 유지 프레임(기상 전 대기)")]
    public int downFrames = 40;

    [Tooltip("미끄러지는 거리(넘어진 직후 수평 잔여 이동). 선택사항")]
    public float slideDistance = 0.0f;

    [Tooltip("slideDistance 분배 프레임")]
    public int slideDurationFrames = 8;

    [Tooltip("기상 가능한 다운인지(소프트KD)")]
    public bool techable = true;
}

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

    [Header("거리 분배 넉백(수평)")]
    public PushProfile push = new PushProfile();

    [Header("넉다운 설정")]
    public KnockdownProfile knockdown = new KnockdownProfile();

    public float driveGaugeChargeAmount;
    public float saGaugeChargeAmount;

    public HitLevel hitLevel = HitLevel.High;

    [Space(20)]
    public string animationClipName;

    [Header("박스 정보")]
    public List<BoxLifetime> boxLifetimes = new();

    [Tooltip("연쇄히트 쿨다운 프레임 수")]
    public int rehitCooldownFrames;

    public string throwAnimationClipName;
    public string beingThrownAnimationClipName;

    [Header("드라이브 정보")]
    public SkillFlag skillFlag;

    //TODO: hit effect, 그냥 이펙트들 등등
}
