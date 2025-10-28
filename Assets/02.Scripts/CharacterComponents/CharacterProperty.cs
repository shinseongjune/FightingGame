using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.PlayerSettings;

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
    private static int _attackInstanceSeq = 1; // 0�� ���ϰ�, match ���� �� 1����
    public static int NextAttackInstanceId()
    {
        return unchecked(++_attackInstanceSeq);
    }

    private TickMaster _tm;

    public string characterName;

    public PhysicsEntity phys;
    public CharacterFSM fsm;
    public InputBuffer input;

    public CharacterStateTag characterStateTag;

    [Header("Skill Settings")]
    public List<Skill_SO> allSkills; // ĳ���Ͱ� ���� ��ü ��ų
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

    // ���� �ߺ� �浹 ���� �ذ��
    public int attackInstanceId;

    public int parryJustEndFrame { get; private set; } = -1;
    public int parryHoldEndFrame { get; private set; } = -1;
    public bool IsInJustParry => Time.frameCount <= parryJustEndFrame;
    public bool IsInParry => Time.frameCount <= parryHoldEndFrame;

    public int parryDisableFrame = 0;
    public int parryLockEndFrame { get; private set; } = -1;
    public bool IsParryLocked => Time.frameCount <= parryLockEndFrame;

    #region === Combo (Defender-Centric) ===
    [Header("=== Combo (Defender-Centric) ===")]
    [Tooltip("��Ʈ �� �ִ� ��� ������(������ �޺� ���)")]
    public int comboKeepFrames = 120;   // 60fps ���� 2��

    [Tooltip("n��° ��Ʈ�� ������ ������ ����(�ε���: 1��Ʈ=0)")]
    public float[] comboDamageTable = new float[] {
        1.00f, 0.90f, 0.85f, 0.80f, 0.75f,
        0.70f, 0.65f, 0.60f, 0.55f, 0.50f,
        0.45f, 0.40f, 0.35f, 0.30f, 0.25f, 0.20f
    };

    [Tooltip("������ ������ �ּ� �ٴ�ġ")]
    public float comboDamageFloor = 0.20f;

    // �޺� ����(�ǰ��� ����)
    [NonSerialized] public int currentComboCount = 0;   // ������� ���� ��Ʈ ��
    [NonSerialized] public float currentComboDamage = 0f; // ���� ������(HUD��)
    [NonSerialized] public int currentComboStartFrame = -1;
    [NonSerialized] public int currentComboDropFrame = -1;
    [NonSerialized] public int lastAttackerId = 0;      // ������ InstanceID
    public Action OnComboChanged;

    public bool HasStoodUpAfterKnockdown { get; private set; }  // ���� �Ϸ� ��ȣ
    bool _postRoundSettle;                                      // ���� ��� �÷���
    float _settleFallSpeed;                                     // ���� ���� ��ǥ�ӵ�
    float _snapEps;                                             // ���� ���� ����
    bool _autoStandOnLand;                                      // ���� �� �ڵ� ���
    public bool IsPostRoundSettling { get; private set; }

    static readonly HashSet<CharacterStateTag> NeutralTagsForComboDrop = new()
    {
        CharacterStateTag.Idle,
        CharacterStateTag.Crouch,
        CharacterStateTag.Guarding,
        CharacterStateTag.Jump_Up,
        CharacterStateTag.Jump_Forward,
        CharacterStateTag.Jump_Backward,
        CharacterStateTag.Walk_Forward,
        CharacterStateTag.Walk_Backward,
        CharacterStateTag.Dash_Forward,
        CharacterStateTag.Dash_Backward,

    };

    public List<Transform> throwHoldOffsets = new(); // ��/�� �� ��

    public void BeginPostRoundSettle(float fastFallY, float snapEps)
    {
        IsPostRoundSettling = true;
        _postRoundSettle = true;
        _autoStandOnLand = true;
        _settleFallSpeed = Mathf.Abs(fastFallY);
        _snapEps = Mathf.Max(0.001f, snapEps);
        HasStoodUpAfterKnockdown = false;

        isInputEnabled = false;
        fsm?.CancelAllBufferedRequests();

        if (phys != null)
        {
            phys.mode = PhysicsMode.Normal;
            phys.isGravityOn = true;

            if (!phys.isGrounded)
            {
                var v = phys.Velocity;
                if (v.y > -_settleFallSpeed) v.y = -_settleFallSpeed;
                v.x *= 0.4f;
                phys.Velocity = v;
            }
        }
    }

    public void TickPostRoundSettle()
    {
        if (!_postRoundSettle || phys == null) return;

        // ���� ����(����� �ſ� ������ ���� ���� �� ����)
        if (!phys.isGrounded && phys.Position.y <= _snapEps)
        {
            phys.Position.y = 0;
            phys.Velocity.y = 0;
            phys.isGrounded = true;
        }

        // ���� ���� ó��
        if (phys.isGrounded)
        {
            // �˴ٿ�/��Ʈ/���߻��� �� �����̵� �� �� �ִ� ���·� ����
            if (_autoStandOnLand)
            {
                ForceStandUpImmediately();
                _autoStandOnLand = false;
                HasStoodUpAfterKnockdown = true; // �� ���� �Ϸ� ��ȣ
            }
        }
    }

    public void ForceStandUpImmediately()
    {
        if (fsm == null) return;

        // ���� Knockdown/BeingThrown/Hitstun ���� ���� �迭�̸� ���� ����
        fsm.ForceExitReactionStates(); // ������ �Ʒ�ó�� ���� ���� ���� ����

        fsm.RequestTransition("WakeUp");
    }

    public Transform GetThrowAnchor(int index)
    {
        if (throwHoldOffsets != null && index >= 0 && index < throwHoldOffsets.Count)
            return throwHoldOffsets[index];
        return null;
    }

    // �޺� ���� ������ ���� (��� ���� ���� �޺��� ������ �ʵ���)
    public void ExtendComboWindow(int extraFrames)
    {
        if (currentComboCount > 0)
            currentComboDropFrame = Mathf.Max(currentComboDropFrame, Time.frameCount + Mathf.Max(1, extraFrames));
    }

    // (����) ĳ����/����� ��ô����
    public bool isThrowInvincible;
    private int throwInvincibleEndFrame = -1;
    public void GrantThrowInvincible(int frames)
    {
        isThrowInvincible = true;
        throwInvincibleEndFrame = Time.frameCount + Mathf.Max(0, frames);
    }
    public void ClearThrowInvincible()
    {
        isThrowInvincible = false;
        throwInvincibleEndFrame = -1;
    }

    public bool IsThrowableNow()
    {
        // ���� ����
        if (!phys.isGrounded) return false;

        // ���� �±� ��� ���� (���ϴ� ��ŭ �߰�)
        switch (characterStateTag)
        {
            case CharacterStateTag.Hit:
            case CharacterStateTag.Guarding:
            case CharacterStateTag.Knockdown:
            case CharacterStateTag.BeingThrown:
            case CharacterStateTag.Throw:
            case CharacterStateTag.Jump_Up:
            case CharacterStateTag.Jump_Forward:
            case CharacterStateTag.Jump_Backward:
                return false;
        }

        // ����/��� ����
        if (isInvincible || isThrowInvincible) return false;
        if (throwInvincibleEndFrame >= 0 && Time.frameCount <= throwInvincibleEndFrame) return false;

        return true;
    }

    public void ResetCombo()
    {
        currentComboCount = 0;
        currentComboDamage = 0f;
        currentComboStartFrame = -1;
        currentComboDropFrame = -1;
        lastAttackerId = 0;

        OnComboChanged?.Invoke();
    }

    public void ForceDropCombo() => ResetCombo();

    public bool IsComboBreakingNeutral(CharacterStateTag? tag)
        => tag.HasValue && NeutralTagsForComboDrop.Contains(tag.Value);

    /// <summary> FSM ���� ���� �� �ܺο��� ȣ��: ��Ʈ���̸� ��� �޺� ��� </summary>
    public void NotifyStateEnterForCombo(CharacterStateTag? tag)
    {
        if (IsComboBreakingNeutral(tag))
            ForceDropCombo();
    }

    /// <summary>
    /// ��Ʈ �� �ݵ�� ȣ��. ������ ���� ����/���� �� �Ǵ� �� �޺� ���� �� �����ϸ� ������ ��ȯ.
    /// </summary>
    public float RegisterComboAndScaleDamage(int attackerInstanceId, float baseDamage)
    {
        int now = Time.frameCount;
        bool sameAttacker = (attackerInstanceId == lastAttackerId);
        bool keepWindow = (now <= currentComboDropFrame);

        if (!sameAttacker || !keepWindow || currentComboCount <= 0)
        {
            // �� �޺� ����
            currentComboCount = 1;
            currentComboDamage = 0f;
            currentComboStartFrame = now;
            lastAttackerId = attackerInstanceId;
        }
        else
        {
            // �޺� ����
            currentComboCount++;
        }

        // ���� ��Ʈ�� ��ٸ� ���� ������ ����
        currentComboDropFrame = now + Mathf.Max(1, comboKeepFrames);
        // n��Ʈ ������
        int idx = Mathf.Clamp(currentComboCount - 1, 0, comboDamageTable.Length - 1);
        float scale = (comboDamageTable.Length > 0) ? comboDamageTable[idx] : 1f;
        scale = Mathf.Max(comboDamageFloor, scale);
        float scaled = baseDamage * scale;
        currentComboDamage += Mathf.Max(0f, scaled);

        OnComboChanged?.Invoke();

        return scaled;
    }
    #endregion

    private void Awake()
    {
        phys = GetComponent<PhysicsEntity>();
        fsm = GetComponent<CharacterFSM>();
        input = GetComponent<InputBuffer>();
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
        TickPostRoundSettle();

        if (fsm?.Current != fsm?.GetState<DriveParryState>("DriveParry"))
        {
            parryDisableFrame = Mathf.Max(0, parryDisableFrame - 1);
        }

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

        // �޺� ���� �ð� ���� �� �ڵ� ���
        if (currentComboCount > 0 && Time.frameCount > currentComboDropFrame)
        {
            ResetCombo();
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

    public void BeginParryWindow(int justFrames, int holdFrames)
    {
        int now = Time.frameCount;
        parryJustEndFrame = now + Mathf.Max(0, justFrames);
        parryHoldEndFrame = now + Mathf.Max(justFrames, holdFrames);
    }

    public void ClearParryWindow()
    {
        parryJustEndFrame = -1;
        parryHoldEndFrame = -1;
    }

    public void BeginParryLockByBlockstun(int blockstunFrames)
    {
        // blockstunFrames == 0 �̸� ��� �����Ǵ� ȿ��
        parryLockEndFrame = Time.frameCount + Mathf.Max(0, blockstunFrames);
    }
    public void ClearParryLock()
    {
        parryLockEndFrame = -1;
    }

    public struct PendingThrowContext
    {
        public CharacterProperty throwerProp;
        public PhysicsEntity targetPhys;     // ������ �ʿ��� �ǹ�
        public Skill_SO skill;               // ����
        public bool has;
    }

    public PendingThrowContext pendingThrow;

    public void SetPendingThrowFromAttacker(PhysicsEntity target, Skill_SO s)
    {
        pendingThrow.targetPhys = target;
        pendingThrow.skill = s;
        pendingThrow.has = true;
    }

    public void SetPendingThrowFromDefender(CharacterProperty thrower, Skill_SO s)
    {
        pendingThrow.throwerProp = thrower;
        pendingThrow.skill = s;
        pendingThrow.has = true;
    }

    public PendingThrowContext ConsumePendingThrow()
    {
        var c = pendingThrow;
        pendingThrow = default; // has=false
        return c;
    }

    public void ResetDriveForNewRound()
    {
        isExhausted = false;
        driveGauge = maxDriveGauge;
        isDriveGaugeCharging = true;
    }
}
