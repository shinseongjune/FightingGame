// Assets/02.Scripts/CharacterComponents/PhysicsComponents/CollisionResolver.cs
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BoxManager�� �Ѹ��� CollisionData(�ڽ� ��)������
/// - ������/�ǰ��� �Ǻ� (�ڽ� Ÿ�� ���)
/// - ��Ʈ/���� Ȯ�� ��, ���� push�� "�Ÿ�/�Ⱓ"���� �й�(velocity �˹� ����)
/// - �ڳ� �й�(�ǰ��� �켱 �� ���� �Ÿ���ŭ ������ ���и�)
/// - ��Ʈ/���� ����, ������, �˴ٿ�(��ġ ����; Trip/PopUpLight��)
/// �� �����Ѵ�.
/// 
/// ����:
/// - BoxType { Body, Hit, Hurt, Throw, GuardTrigger }
/// - PhysicsEntity: Position, Velocity, currentBodyBox, SyncTransform(), isGrounded ��
/// - CharacterProperty: isFacingRight, IsGrounded, SetHitstun(int, Vector2), SetBlockstun(int),
///                      ApplyDamage(float), EnterKnockdown(), currentSkill ��
/// - StageSetup: bounds.leftX/rightX (�� ���)
/// - TickMaster.TICK_INTERVAL
/// </summary>
[RequireComponent(typeof(PhysicsEntity))]
[RequireComponent(typeof(CharacterProperty))]
public sealed class CollisionResolver : MonoBehaviour, ITicker
{
    #region Types
    enum PairKind { None, Hit, Throw, GuardTrigger }

    struct FrameEvent
    {
        public PairKind kind;
        public CollisionData cd;

        public PhysicsEntity attacker;
        public PhysicsEntity defender;

        public CharacterProperty atkProp;
        public CharacterProperty defProp;

        public Skill_SO skill;
        public bool defenderAir;

        // ��ġ
        public int hitstun;
        public int blockstun;
        public float damage;

        // ����(+1/-1): ������ ���̽� ����
        public float dir;

        // ���� �����ӿ� GuardTrigger�� ���� ���� ��� ǥ��
        public bool guardTouch;
    }

    // �Ÿ� �й� ��û(�ǰ��� �������� ����; �����ڴ� ���и������� �Բ� �̵�)
    struct PushJob
    {
        public PhysicsEntity attacker;
        public PhysicsEntity defender;
        public float dir;           // +1/-1 (������ ���̽�)
        public float remainDist;    // ���� �̰� �Ÿ�(>0)
        public int framesLeft;      // ���� ������(>=1)
        public int totalFrames;
        public int easingType;      // 0=linear, 1=easeOutCubic, 2=easeInCubic, 3=easeInOutCubic
    }
    #endregion

    #region Refs
    private PhysicsEntity _me;
    private CharacterProperty _prop;
    private InputBuffer inputBuffer;
    private BoxManager _box;
    private StageSetup _stage;
    private StageSetup.StageBounds _bounds;
    #endregion

    #region Buffers
    private readonly List<FrameEvent> _events = new(8);
    private readonly List<PushJob> _pushJobs = new(8);
    private int _frame;
    private readonly Dictionary<(int atk, int def, int uid), int> _rehitUntil = new();
    #endregion

    #region Defaults & Tuning
    private const float DefaultHitDistance = 0.45f;
    private const int DefaultHitFrames = 8;
    private const float DefaultGuardDistance = 0.30f;
    private const int DefaultGuardFrames = 6;
    private const float DefaultAirHitDistance = 0.35f;
    private const int DefaultAirHitFrames = 6;

    private const float PopUpLightVy = 3.5f; // KnockdownMode.PopUpLight�� ���� �ҷ� �ο�

    // skill.knockbackVelocity�� �Ÿ��� ȯ���� �� ����� �ӽ� ������ ��(�ɼ�)
    private const int VelocityToDistanceFrames = 6;
    #endregion

    float InnerRightX => _bounds.rightX - _stage.wallThickness * 0.5f;
    float InnerLeftX => _bounds.leftX + _stage.wallThickness * 0.5f;

    #region Unity
    private void Awake()
    {
        _me = GetComponent<PhysicsEntity>();
        _prop = GetComponent<CharacterProperty>();
        inputBuffer = GetComponent<InputBuffer>();
    }

    private void OnEnable()
    {
        _box = BoxManager.Instance;
        if (_box != null) _box.OnCollision += OnCollisionFromBoxManager;

        _stage = FindFirstObjectByType<StageSetup>();
        if (_stage != null) _bounds = _stage.bounds;
    }

    private void OnDisable()
    {
        if (_box != null) _box.OnCollision -= OnCollisionFromBoxManager;
        _box = null;
    }
    #endregion

    #region BoxManager �� collect
    private void OnCollisionFromBoxManager(CollisionData cd)
    {
        if (cd?.boxA == null || cd.boxB == null) return;

        // �� ĳ���Ͱ� ���õ� �浹�� ����
        if (cd.boxA.owner != _me && cd.boxB.owner != _me) return;

        // ���� �з��ϰ� ������/�ǰ��� ����
        if (!Classify(cd, out PairKind kind, out var atkBox, out var defBox)) return;

        var atk = atkBox.owner;
        var def = defBox.owner;
        if (atk == null || def == null) return;

        var atkProp = atk.GetComponent<CharacterProperty>();
        var defProp = def.GetComponent<CharacterProperty>();
        if (atkProp == null || defProp == null) return;

        // ����(+1/-1): ������ ���̽� ����
        float dir = atkProp.isFacingRight ? +1f : -1f;

        // ���/��ġ: Skill_SO�� �ڽ��� sourceSkill �Ǵ� ������ currentSkill���� ���
        var skill = atkBox.sourceSkill != null ? atkBox.sourceSkill : atkProp.currentSkill;
        int hitstun = skill != null ? Mathf.Max(0, skill.hitstunDuration) : 0;
        int blockstun = skill != null ? Mathf.Max(0, skill.blockstunDuration) : 0;
        float damage = skill != null ? Mathf.Max(0, skill.damageOnHit) : 0f;

        bool defAir = !defProp.phys.isGrounded;

        // GuardTrigger�� ���� ������ �� guardTouch ǥ�ÿ�
        bool guardTouch = (kind == PairKind.GuardTrigger);

        _events.Add(new FrameEvent
        {
            kind = kind,
            cd = cd,
            attacker = atk,
            defender = def,
            atkProp = atkProp,
            defProp = defProp,
            skill = skill,
            defenderAir = defAir,
            hitstun = hitstun,
            blockstun = blockstun,
            damage = damage,
            dir = dir,
            guardTouch = guardTouch
        });
    }

    private static bool Classify(CollisionData cd, out PairKind kind, out BoxComponent attacker, out BoxComponent defender)
    {
        var a = cd.boxA; var b = cd.boxB;
        kind = PairKind.None; attacker = defender = null;

        if (IsPair(a, b, BoxType.Hit, BoxType.Hurt))
        {
            kind = PairKind.Hit;
            (attacker, defender) = a.type == BoxType.Hit ? (a, b) : (b, a);
            return true;
        }
        if (IsPair(a, b, BoxType.Throw, BoxType.Hurt))
        {
            kind = PairKind.Throw;
            (attacker, defender) = a.type == BoxType.Throw ? (a, b) : (b, a);
            return true;
        }
        if (IsPair(a, b, BoxType.GuardTrigger, BoxType.Hurt))
        {
            kind = PairKind.GuardTrigger;
            (attacker, defender) = a.type == BoxType.GuardTrigger ? (a, b) : (b, a);
            return true;
        }
        return false;
    }

    private static bool IsPair(BoxComponent a, BoxComponent b, BoxType ta, BoxType tb)
        => (a.type == ta && b.type == tb) || (a.type == tb && b.type == ta);
    #endregion

    #region Tick (deterministic)
    public void Tick()
    {
        _frame++;

        if (_events.Count > 0)
        {
            // 1) GuardTrigger�� ���� ��ĵ�ؼ�, ���� (atk,def) ���� Hit�� guardTouch �÷��׸� ����
            MarkGuardTouches();

            // 2) Throw/Hit ó��
            for (int i = 0; i < _events.Count; i++)
            {
                var ev = _events[i];

                switch (ev.kind)
                {
                    case PairKind.Throw:
                        ApplyThrow(ev);
                        break;

                    case PairKind.Hit:
                        ApplyHitOrBlock(ev);
                        break;

                        // GuardTrigger�� ������ guardTouch ��ŷ�� �ϰ� �� ó�� ����
                }
            }

            _events.Clear();
        }

        // 3) �й� ť ó��(��� PushJob�� �� ������ �й�)
        if (_pushJobs.Count > 0)
        {
            for (int i = _pushJobs.Count - 1; i >= 0; --i)
            {
                var job = _pushJobs[i];
                float step = ComputeStep(job);

                float consumed = ApplyCornerSplit(job.attacker, job.defender, job.dir, step);

                job.remainDist = Mathf.Max(0f, job.remainDist - consumed);
                job.framesLeft = Mathf.Max(0, job.framesLeft - 1);

                if (job.remainDist <= 1e-5f || job.framesLeft <= 0)
                    _pushJobs.RemoveAt(i);
                else
                    _pushJobs[i] = job;
            }
        }
    }
    #endregion

    #region Apply: Guard mark / Throw / Hit-Block / Push
    private void MarkGuardTouches()
    {
        // ������: �� ������ guardTrigger�� ���� (atk,def) ����� �����.
        // ���� (atk,def)�� Hit �̺�Ʈ�� guardTouch=true�� OR ��Ų��.
        Span<(int atk, int def)> tmp = stackalloc (int, int)[_events.Count];
        int n = 0;

        for (int i = 0; i < _events.Count; i++)
        {
            if (_events[i].kind != PairKind.GuardTrigger) continue;
            tmp[n++] = (_events[i].attacker.GetInstanceID(), _events[i].defender.GetInstanceID());
        }
        if (n == 0) return;

        for (int i = 0; i < _events.Count; i++)
        {
            var ev = _events[i];
            if (ev.kind != PairKind.Hit) continue;

            int atkId = ev.attacker.GetInstanceID();
            int defId = ev.defender.GetInstanceID();

            for (int k = 0; k < n; k++)
            {
                if (tmp[k].atk == atkId && tmp[k].def == defId)
                {
                    ev.guardTouch = true;
                    _events[i] = ev;
                    break;
                }
            }
        }
    }

    private void ApplyThrow(in FrameEvent ev)
    {
        // ���� ����(���� Throw/BeingThrown ������ ����)
        if (ev.defender == _me)
        {
            var defFSM = ev.defender.GetComponent<CharacterFSM>();
            defFSM?.TransitionTo("BeingThrown");
            (defFSM?.Current as BeingThrownState)?.SetTrower(ev.attacker.property);
        }
        else if (ev.attacker == _me)
        {
            var atFSM = ev.attacker.GetComponent<CharacterFSM>();
            atFSM?.TransitionTo("Throw");
            (atFSM?.Current as ThrowState)?.SetTarget(ev.defender);
        }
    }

    private void ApplyHitOrBlock(in FrameEvent ev)
    {
        // ��Ƽ��Ʈ ����(��Ʈ�ڽ� UID ���)
        var hitBox = ev.cd.boxA.type == BoxType.Hit ? ev.cd.boxA : ev.cd.boxB;
        int uid = hitBox.uid;
        int atkId = ev.attacker.GetInstanceID();
        int defId = ev.defender.GetInstanceID();

        int cd = ev.skill != null ? ev.skill.rehitCooldownFrames : 0;
        if (_rehitUntil.TryGetValue((atkId, defId, uid), out int next) && _frame < next)
            return; // ���� ����Ʈ ����
        _rehitUntil[(atkId, defId, uid)] = _frame + Mathf.Max(1, cd);

        // ���� ����: GuardTrigger ���� + �ǰ��ڰ� ������ ���带 ����(�Է�) ���� ��
        bool blocked = ev.guardTouch && IsHoldingGuard(ev.defProp, ev.atkProp);

        // ���� & ������
        if (blocked)
        {
            if (ev.blockstun > 0)
            {
                ev.defProp.SetBlockstun(ev.blockstun);
                ev.defProp.fsm.TransitionTo("GuardHit");
            }
        }
        else
        {
            if (ev.damage > 0f) ev.defProp.ApplyDamage(ev.damage);
            if (ev.hitstun > 0)
            {
                ev.defProp.SetHitstun(ev.hitstun, Vector2.zero);
                if (ev.skill.knockdown.mode == KnockdownMode.None)
                {
                    if (ev.defProp.phys.isGrounded)
                    {
                        ev.defProp.fsm.TransitionTo("HitGround");
                    }
                    else
                    {
                        ev.defProp.fsm.TransitionTo("HitAir");
                    }
                }           
            }
        }

        // ���� push �й� ��û(velocity ��� �� ��)
        EnqueuePush(ev, blocked);

        // �˴ٿ�
        if (!blocked && ev.skill != null && ev.skill.knockdown.mode != KnockdownMode.None)
        {
            ApplyKnockdown(ev.defender, ev.defProp, ev.skill);
        }
    }

    private void EnqueuePush(in FrameEvent ev, bool blocked)
    {
        float dist; int frames; int easing;
        GetPushSpec(ev, blocked, out dist, out frames, out easing);
        if (dist <= 1e-5f || frames <= 0) return;

        // �� �������� "��(_me)�� ���õ� �̺�Ʈ��" �����Ƿ�,
        // defender�� ���� ������ �й� ť�� �ø���. (��� ĳ���͵� �ڱ� ���������� ��ü ť�� ����)
        if (ev.defender != _me) return;

        _pushJobs.Add(new PushJob
        {
            attacker = ev.attacker,
            defender = ev.defender,
            dir = ev.dir,
            remainDist = dist,
            framesLeft = frames,
            totalFrames = frames,
            easingType = easing
        });
    }

    private void GetPushSpec(in FrameEvent ev, bool blocked, out float dist, out int frames, out int easingType)
    {
        // Skill_SO�� �Ÿ�/�Ⱓ �ʵ尡 �����Ƿ� �ӽ� �⺻�� ���.
        // �ʿ� �� skill.knockbackVelocity�� �Ÿ��� ȯ���Ͽ� ����.
        easingType = 0;

        if (blocked)
        {
            dist = DefaultGuardDistance;
            frames = DefaultGuardFrames;
        }
        else if (ev.defenderAir)
        {
            dist = DefaultAirHitDistance;
            frames = DefaultAirHitFrames;
        }
        else
        {
            dist = DefaultHitDistance;
            frames = DefaultHitFrames;
        }
    }
    #endregion

    #region Corner split & easing
    private float ComputeStep(in PushJob job)
    {
        if (job.easingType == 0) // linear
            return job.remainDist / job.framesLeft;

        float tPrev = 1f - (job.framesLeft / (float)job.totalFrames);
        float tNow = 1f - ((job.framesLeft - 1) / (float)job.totalFrames);
        float ePrev = Ease(job.easingType, tPrev);
        float eNow = Ease(job.easingType, tNow);
        float frac = Mathf.Max(0f, eNow - ePrev);

        // �й� ���� ������ ���̱� ���� �ּ� ����
        float linearMin = job.remainDist / job.framesLeft * 0.5f;
        return Mathf.Max(frac * job.remainDist, linearMin);
    }

    private static float Ease(int type, float x)
    {
        x = Mathf.Clamp01(x);
        switch (type)
        {
            case 1: // easeOutCubic
                return 1f - Mathf.Pow(1f - x, 3f);
            case 2: // easeInCubic
                return x * x * x;
            default: // 3: easeInOutCubic
                if (x < 0.5f) return 4f * x * x * x;
                return 1f - Mathf.Pow(-2f * x + 2f, 3f) / 2f;
        }
    }

    /// <summary>
    /// �ǰ���(defender)�� �켱 dir �������� �а�, ������ ���� �������� ������(attacker)�� ���������� �̵�.
    /// ���� �̵�(������+������)�� ���� ��ȯ(= �Һ�� �Ÿ�).
    /// </summary>
    private float ApplyCornerSplit(PhysicsEntity attacker, PhysicsEntity defender, float dir, float desired)
    {
        if (desired <= 0f || defender == null) return 0f;

        if (_stage == null)
        {
            defender.Position += new Vector2(dir * desired, 0f);
            defender.SyncTransform();
            return desired;
        }

        Rect defAabb = GetBodyAABB(defender);

        // �� ���� ���� ����� "�ǳ��� ����" ��������
        float defFree = (dir > 0f)
            ? Mathf.Max(0f, InnerRightX - defAabb.xMax)
            : Mathf.Max(0f, defAabb.xMin - InnerLeftX);

        float defMove = Mathf.Min(desired, defFree);
        if (defMove > 0f)
        {
            defender.Position += new Vector2(dir * defMove, 0f);
            defender.SyncTransform();

            // �� �̵� �� ���� Ŭ����(��ħ ������)
            defAabb = GetBodyAABB(defender);
            if (dir > 0f && defAabb.xMax > InnerRightX)
            {
                float fix = defAabb.xMax - InnerRightX;
                defender.Position += new Vector2(-fix, 0f);
                defender.SyncTransform();
            }
            else if (dir < 0f && defAabb.xMin < InnerLeftX)
            {
                float fix = InnerLeftX - defAabb.xMin;
                defender.Position += new Vector2(fix, 0f);
                defender.SyncTransform();
            }
        }

        float remainder = desired - defMove;
        float atkMove = 0f;

        if (remainder > 1e-6f && attacker != null)
        {
            Rect atkAabb = GetBodyAABB(attacker);

            float atkFree = (dir > 0f)
                ? Mathf.Max(0f, atkAabb.xMin - InnerLeftX)
                : Mathf.Max(0f, InnerRightX - atkAabb.xMax);

            atkMove = Mathf.Min(remainder, atkFree);
            if (atkMove > 0f)
            {
                attacker.Position += new Vector2(-dir * atkMove, 0f);
                attacker.SyncTransform();

                // �����ڵ� Ŭ����
                atkAabb = GetBodyAABB(attacker);
                if (dir > 0f && atkAabb.xMin < InnerLeftX)
                {
                    float fix = InnerLeftX - atkAabb.xMin;
                    attacker.Position += new Vector2(fix, 0f);
                    attacker.SyncTransform();
                }
                else if (dir < 0f && atkAabb.xMax > InnerRightX)
                {
                    float fix = atkAabb.xMax - InnerRightX;
                    attacker.Position += new Vector2(-fix, 0f);
                    attacker.SyncTransform();
                }
            }
        }

        return defMove + atkMove;
    }

    private static Rect GetBodyAABB(PhysicsEntity pe)
    {
        var b = pe.currentBodyBox;
        if (b != null) return b.GetAABB();
        // ���� ����ġ
        return new Rect(pe.Position.x - 0.25f, pe.Position.y, 0.5f, 1f);
    }
    #endregion

    #region Guard / KD helpers
    // ������ ���� "Back/DownBack" ���� ���� �Ǵ�
    private bool IsHoldingGuard(CharacterProperty defender, CharacterProperty attacker)
    {
        var dir = inputBuffer.LastInput.direction;
        return dir == Direction.Back || dir == Direction.DownBack;
    }

    private void ApplyKnockdown(PhysicsEntity defender, CharacterProperty defProp, Skill_SO skill)
    {
        if (defender == null || defProp == null || skill == null) return;

        var mode = skill.knockdown.mode;
        switch (mode)
        {
            case KnockdownMode.PopUp:
                var v = defender.Velocity;
                v.y = Mathf.Max(v.y, PopUpLightVy);
                defender.Velocity = v;
                defProp.fsm.TransitionTo("Knockdown");
                break;

            default: // Trip
                defProp.fsm.TransitionTo("Knockdown");
                break;
        }
    }
    #endregion
}
