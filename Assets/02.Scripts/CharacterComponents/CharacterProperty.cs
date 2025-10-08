using System.Collections.Generic;
using UnityEngine;

public enum CharacterStateTag
{
    None,
    Idle,
    Crouch,
    Jump_Up,
    Jump_Forward,
    Jump_Backward,
    Walk_Forward,
    Walk_Backward,
    Dash_Forward,
    Dash_Backward,
    Skill,
    Hit,
    Hit_Air,
    Hit_Guard,
    Guarding,
    Knockdown,
    HardKnockdown,
    DriveImpact,
    DriveRush,
    DriveRushSkill,
    DriveParry,
    DriveReversal,
    Throw,
    BeingThrown,
    ForcedAnimation,
}

public class CharacterProperty : MonoBehaviour, ITicker
{
    private TickMaster _tm;

    public string characterName;

    public PhysicsEntity phys;
    public CharacterFSM fsm;

    public CharacterStateTag characterStateTag;

    [Header("Skill Settings")]
    public List<Skill_SO> allSkills; // 캐릭터가 가진 전체 스킬
    public Skill_SO currentSkill;

    public readonly List<GameObject> projectiles;

    public bool isFacingRight;

    public bool isStartUp;
    public bool isRecovery;
    public bool isInputEnabled = true;
    public bool isSkillCancelable;
    public bool isInvincible;
    public bool isAirInvincible;
    public bool isProjectileInvincible;
    public bool isRushCanceled;
    public int superArmorCount;

    public float hp;
    public float maxHp = 10000;

    public float saGauge;
    public float maxSAGauge = 300;

    public bool isExhausted;
    public bool isDriveGaugeCharging;
    public float driveGauge;
    public float maxDriveGauge = 600;
    public float driveGaugeTickChargeAmount = 25/60;

    public int pendingHitstunFrames;
    public int pendingBlockstunFrames;
    public Vector2 pendingKnockback;

    // 공격 중복 충돌 문제 해결용
    public int attackInstanceId;

    private void Awake()
    {
        phys = GetComponent<PhysicsEntity>();
        fsm = GetComponent<CharacterFSM>();
    }

    private void Start()
    {
        isDriveGaugeCharging = true;
        driveGauge = maxDriveGauge;
    }

    private void OnEnable()
    {
        _tm = TickMaster.Instance;
        _tm?.Register(this);
    }

    private void OnDisable()
    {
        _tm?.Unregister(this);
    }

    public void Tick()
    {
        if (driveGauge <= 0)
        {
            isExhausted = true;
        }
        else if (isExhausted && driveGauge >= maxDriveGauge)
        {
            isExhausted = false;
        }

        if (isExhausted || isDriveGaugeCharging)
        {
            ChargeDriveGauge(driveGaugeTickChargeAmount);
        }
    }

    public void SetHitstun(int frames, Vector2 kb)
    {
        pendingHitstunFrames = frames;
        pendingKnockback = kb;
    }
    public void SetBlockstun(int frames)
    {
        pendingBlockstunFrames = frames;
    }

    public void ChargeSAGauge(float amount)
    {
        saGauge = Mathf.Min(saGauge + amount, maxSAGauge);
    }

    public void ConsumeSAGauge(float amount)
    {
        saGauge = Mathf.Max(0, saGauge - amount);
    }

    public void ChargeDriveGauge(float amount)
    {
        driveGauge = Mathf.Min(driveGauge + amount, maxDriveGauge);
        if (driveGauge <= 0)
        {
            isExhausted = true;
        }
    }

    public void ConsumeDriveGauge(float amount)
    {
        driveGauge = Mathf.Max(0, driveGauge - amount);
    }

    public void SetFacing(bool facingRight)
    {
        //if (isFacingRight == facingRight) return;
        isFacingRight = facingRight;

        var e = transform.localEulerAngles;
        e.y = facingRight ? 90 : -90;

        transform.localEulerAngles = e;

        phys.SetPose(characterStateTag);
    }

    public void SpawnAt(Vector2 worldPos, bool initialFacing)
    {
        phys.Position = worldPos;
        phys.SyncTransform();
        SetFacing(initialFacing);
    }

    public void ApplyDamage(float damage)
    {
        hp = Mathf.Max(0, hp - damage);
    }
}
