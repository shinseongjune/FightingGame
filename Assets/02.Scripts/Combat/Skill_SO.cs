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
    public int easingType = 0; // 0=����, 1/2/3=ease
}

public enum KnockdownMode
{
    None,   // �˴ٿ� �ƴ�
    Trip,   // �ٴ����� �׳� �ѱ�(���� ��ȭ X)
    PopUp,  // ��¦ ����� �ѱ�(ª�� ����Ʈ��)
}

[System.Serializable]
public class KnockdownProfile
{
    public KnockdownMode mode = KnockdownMode.None;

    [Tooltip("PopUp�� �� ���� ����(����). ���� �۰�(��: 0.4~0.7)")]
    public float popUpApexHeight = 0.5f;

    [Tooltip("�Ѿ��� �� ���� �ٿ� ���� ������(��� �� ���)")]
    public int downFrames = 40;

    [Tooltip("�̲������� �Ÿ�(�Ѿ��� ���� ���� �ܿ� �̵�). ���û���")]
    public float slideDistance = 0.0f;

    [Tooltip("slideDistance �й� ������")]
    public int slideDurationFrames = 8;

    [Tooltip("��� ������ �ٿ�����(����ƮKD)")]
    public bool techable = true;
}

[Serializable]
public struct ThrowTimeline
{
    [Tooltip("�ǰ��� ������/������ ���� ������(�ִ� ���� ƽ ������)")]
    public int impactFrame;

    [Tooltip("�ǰ��ڸ� �տ��� ���� ������(������)")]
    public int releaseFrame;

    [Tooltip("������ �� ���� �߻� �ӵ� (������ ���� �¿� ��ȣ �����)")]
    public Vector2 launchVelocity;

    [Tooltip("��ô ������")]
    public float damage;

    [Tooltip("������ ���� �ٿ� ����/��Ʈ���� ������ �� �ʿ�� Ȯ��")]
    public int postHitstunFrames;
    public bool hardKnockdown;

    [Tooltip("���� ��Ŀ �ε��� (CharacterProperty.throwHoldOffsets)")]
    public int holdAnchorIndex;

    [Tooltip("��� ���� ���� �޺� ������ �ּ� �� ������ ��������")]
    public int comboLockFrames;

    [Tooltip("��� ���� ��� ��ġ�� ��(Transform)�� Attach���� ����")]
    public bool useAttachFollow;
}

[Serializable]
public struct ProjectileSpawnEvent
{
    [Tooltip("�� �ִϸ��̼� ������(clip.frameRate ����)�� ����")]
    public int frame;

    [Tooltip("������ ������(ProjectileController�� �޷��־�� ��)")]
    public GameObject prefab;

    [Tooltip("ĳ���� ���� �̸�(������ �� ���ڿ�)")]
    public string socketName;

    [Tooltip("����(�Ǵ� ��Ʈ) ���� ���� ������")]
    public Vector3 localOffset;

    [Tooltip("�ʱ� �ӵ�(ĳ���� ���̽��� �������� �¿� ���� ó����)")]
    public Vector2 initialVelocity;

    [Tooltip("�߷� ������(0�̸� �߷� ����)")]
    public float gravityScale;

    [Tooltip("���� �ð�(��). 0 ���ϸ� ����, ���� �������� ����")]
    public float lifeTimeSec;

    [Tooltip("����ü�� ����� Skill(��Ʈ ��ġ/�ڽ� Ÿ�Ӷ��� ����). ������ ������ currentSkill�� ���")]
    public Skill_SO projectileSkill;

    [Tooltip("��Ʈ �� �ı����� ����")]
    public bool destroyOnHit;
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

    [Header("�Ÿ� �й� �˹�(����)")]
    public PushProfile push = new PushProfile();

    [Header("�˴ٿ� ����")]
    public KnockdownProfile knockdown = new KnockdownProfile();

    public float driveGaugeChargeAmount;
    public float saGaugeChargeAmount;

    public HitLevel hitLevel = HitLevel.High;

    [Space(20)]
    public string animationClipName;

    [Header("�ڽ� ����")]
    public List<BoxLifetime> boxLifetimes = new();

    [Tooltip("������Ʈ ��ٿ� ������ ��")]
    public int rehitCooldownFrames;

    public string throwAnimationClipName;
    public string beingThrownAnimationClipName;

    [Header("����̺� ����")]
    public SkillFlag skillFlag;

    [Header("Throw Timeline")]
    public ThrowTimeline throwCfg;

    [Header("Projectile")]
    public bool spawnsProjectiles;
    public ProjectileSpawnEvent[] projectileSpawns;

    [Header("VFX Keys (optional; empty -> use defaults)")]
    public string startVfxKey;   // ��� ���۽� (��: ���� ����)
    public string hitVfxKey;     // �� ����� ������ �� (���� ��� ���� ����ũ)
    public string guardVfxKey;   // �� ����� ����� �� (���� ��� ���� ����ũ)

    [Header("Mid-Skill FX Cues (frame-based)")]
    public List<FxCue> fxCues = new(); // �ִϸ��̼�/�����ӿ� ���� �߰� ����Ʈ Ʈ���� (���� ����)

    [System.Serializable]
    public struct FxCue
    {
        public string key;           // ���̺귯�� Ű
        public int frame;            // ���� ���� frame(ƽ) ���� (0 ����)
        public string attachBone;    // ���� ������ǥ
        public Vector3 offset;       // ��/��Ʈ ���� ������
        public bool follow;          // true�� ��/��Ʈ�� ����
        public bool worldSpace;      // true�� ����, false�� ����
    }
}
