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

[Serializable]
public struct ThrowTimeline
{
    [Tooltip("피격자 데미지/게이지 적용 프레임(애니 기준 틱 프레임)")]
    public int impactFrame;

    [Tooltip("피격자를 손에서 놓는 프레임(릴리즈)")]
    public int releaseFrame;

    [Tooltip("릴리즈 시 최종 발사 속도 (시전자 기준 좌우 부호 적용됨)")]
    public Vector2 launchVelocity;

    [Tooltip("투척 데미지")]
    public float damage;

    [Tooltip("릴리즈 이후 다운 여부/히트스턴 프레임 등 필요시 확장")]
    public int postHitstunFrames;
    public bool hardKnockdown;

    [Tooltip("따라갈 앵커 인덱스 (CharacterProperty.throwHoldOffsets)")]
    public int holdAnchorIndex;

    [Tooltip("잡기 연출 동안 콤보 유예를 최소 몇 프레임 보장할지")]
    public int comboLockFrames;

    [Tooltip("잡기 동안 대상 위치를 본(Transform)에 Attach할지 여부")]
    public bool useAttachFollow;
}

[Serializable]
public struct ProjectileSpawnEvent
{
    [Tooltip("이 애니메이션 프레임(clip.frameRate 기준)에 스폰")]
    public int frame;

    [Tooltip("스폰할 프리팹(ProjectileController가 달려있어야 함)")]
    public GameObject prefab;

    [Tooltip("캐릭터 소켓 이름(없으면 빈 문자열)")]
    public string socketName;

    [Tooltip("소켓(또는 루트) 기준 로컬 오프셋")]
    public Vector3 localOffset;

    [Tooltip("초기 속도(캐릭터 페이싱을 기준으로 좌우 반전 처리됨)")]
    public Vector2 initialVelocity;

    [Tooltip("중력 스케일(0이면 중력 없음)")]
    public float gravityScale;

    [Tooltip("생존 시간(초). 0 이하면 무한, 별도 조건으로 제거")]
    public float lifeTimeSec;

    [Tooltip("투사체가 사용할 Skill(히트 수치/박스 타임라인 포함). 없으면 시전자 currentSkill을 사용")]
    public Skill_SO projectileSkill;

    [Tooltip("히트 시 파괴할지 여부")]
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

    [Header("Throw Timeline")]
    public ThrowTimeline throwCfg;

    [Header("Projectile")]
    public bool spawnsProjectiles;
    public ProjectileSpawnEvent[] projectileSpawns;

    [Header("VFX Keys (optional; empty -> use defaults)")]
    public string startVfxKey;   // 기술 시작시 (예: 베기 오라)
    public string hitVfxKey;     // 이 기술이 적중할 때 (개별 기술 전용 스파크)
    public string guardVfxKey;   // 이 기술이 가드될 때 (개별 기술 전용 스파크)

    [Header("Mid-Skill FX Cues (frame-based)")]
    public List<FxCue> fxCues = new(); // 애니메이션/프레임에 맞춰 중간 이펙트 트리거 (잡기용 포함)

    [System.Serializable]
    public struct FxCue
    {
        public string key;           // 라이브러리 키
        public int frame;            // 상태 진행 frame(틱) 기준 (0 시작)
        public string attachBone;    // 비우면 월드좌표
        public Vector3 offset;       // 본/루트 기준 오프셋
        public bool follow;          // true면 본/루트에 따라감
        public bool worldSpace;      // true면 월드, false면 로컬
    }
}
