// Assets/02.Scripts/CharacterComponents/PhysicsComponents/CollisionResolver.cs
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BoxManager가 뿌리는 CollisionData(박스 쌍)만으로
/// - 공격자/피격자 판별 (박스 타입 기반)
/// - 히트/가드 확정 후, 수평 push를 "거리/기간"으로 분배(velocity 넉백 제거)
/// - 코너 분배(피격자 우선 → 남은 거리만큼 공격자 역밀림)
/// - 히트/가드 스턴, 데미지, 넉다운(런치 없음; Trip/PopUpLight만)
/// 를 적용한다.
/// 
/// 의존:
/// - BoxType { Body, Hit, Hurt, Throw, GuardTrigger }
/// - PhysicsEntity: Position, Velocity, currentBodyBox, SyncTransform(), isGrounded 등
/// - CharacterProperty: isFacingRight, IsGrounded, SetHitstun(int, Vector2), SetBlockstun(int),
///                      ApplyDamage(float), EnterKnockdown(), currentSkill 등
/// - StageSetup: bounds.leftX/rightX (월 경계)
/// - TickMaster.TICK_INTERVAL
/// </summary>
[RequireComponent(typeof(PhysicsEntity))]
[RequireComponent(typeof(CharacterProperty))]
public sealed class CollisionResolver : MonoBehaviour, ITicker
{
    public bool enableResolve = true; // 디버그용

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

        // 수치
        public int hitstun;
        public int blockstun;
        public float damage;

        // 방향(+1/-1): 공격자 페이싱 기준
        public float dir;

        // 같은 프레임에 GuardTrigger를 먼저 받은 경우 표시
        public bool guardTouch;

        public bool parryAttempt;   // 패리 윈도우에 있었는지
        public bool justParry;      // 저스트 패리였는지
    }

    // 거리 분배 요청(피격자 기준으로 저장; 공격자는 역밀림용으로 함께 이동)
    struct PushJob
    {
        public PhysicsEntity attacker;
        public PhysicsEntity defender;
        public float dir;           // +1/-1 (공격자 페이싱)
        public float remainDist;    // 남은 이격 거리(>0)
        public int framesLeft;      // 남은 프레임(>=1)
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

    private const float PopUpLightVy = 3.5f; // KnockdownMode.PopUpLight일 때만 소량 부여

    // skill.knockbackVelocity를 거리로 환산할 때 사용할 임시 프레임 수(옵션)
    private const int VelocityToDistanceFrames = 6;
    #endregion

    float InnerRightX => _bounds.rightX - _stage.wallThickness * 0.5f;
    float InnerLeftX => _bounds.leftX + _stage.wallThickness * 0.5f;
    // === [HITSTOP] Helpers: CollisionResolver 내부에 추가 ===

    static int s_lastAppliedFrame = -1;
    static readonly HashSet<int> s_appliedAttackIdsThisFrame = new HashSet<int>();

    static readonly Dictionary<int, HashSet<int>> s_hitOnceRegistry = new Dictionary<int, HashSet<int>>();


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

    #region BoxManager → collect
    private void OnCollisionFromBoxManager(CollisionData cd)
    {
        if (cd?.boxA == null || cd.boxB == null) return;

        // 내 캐릭터가 관련된 충돌만 관심
        if (cd.boxA.owner != _me && cd.boxB.owner != _me) return;

        // 쌍을 분류하고 공격자/피격자 결정
        if (!Classify(cd, out PairKind kind, out var atkBox, out var defBox)) return;

        var atk = atkBox.owner;
        var def = defBox.owner;
        if (atk == null || def == null) return;

        var atkProp = atk.GetComponent<CharacterProperty>();
        var defProp = def.GetComponent<CharacterProperty>();
        if (atkProp == null || defProp == null) return;

        // 방향(+1/-1): 공격자 페이싱 기준
        float dir = atkProp.isFacingRight ? +1f : -1f;

        // 기술/수치: Skill_SO는 박스에 sourceSkill 또는 공격자 currentSkill에서 취득
        var skill = atkBox.sourceSkill != null ? atkBox.sourceSkill : atkProp.currentSkill;
        int hitstun = skill != null ? Mathf.Max(0, skill.hitstunDuration) : 0;
        int blockstun = skill != null ? Mathf.Max(0, skill.blockstunDuration) : 0;
        float damage = skill != null ? Mathf.Max(0, skill.damageOnHit) : 0f;

        bool defAir = !defProp.phys.isGrounded;

        // GuardTrigger는 같은 프레임 내 guardTouch 표시용
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
        if (!enableResolve) return;

        _frame++;

        if (_events.Count > 0)
        {
            // 1) GuardTrigger를 먼저 스캔해서, 같은 (atk,def) 쌍의 Hit에 guardTouch 플래그를 전달
            MarkGuardTouches();

            // 2) Throw/Hit 처리
            for (int i = 0; i < _events.Count; i++)
            {
                var ev = _events[i];

                int attackInstanceId = ev.atkProp.attackInstanceId;
                int defenderUid = ev.defProp.gameObject.GetInstanceID();

                if (AlreadyHitThisAttack(attackInstanceId, defenderUid))
                {
                    continue;
                }

                MarkHitThisAttack(attackInstanceId, defenderUid);

                switch (ev.kind)
                {
                    case PairKind.Throw:
                        ApplyThrow(ev);
                        break;

                    case PairKind.Hit:
                        ApplyHitOrBlock(ev);
                        break;

                        // GuardTrigger는 위에서 guardTouch 마킹만 하고 별 처리 없음
                }
            }

            _events.Clear();
        }

        // 3) 분배 큐 처리(모든 PushJob을 한 프레임 분배)
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
        // 간단히: 이 프레임 guardTrigger가 닿은 (atk,def) 목록을 만든다.
        // 같은 (atk,def)의 Hit 이벤트에 guardTouch=true를 OR 시킨다.
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
        // 상태 전이(기존 Throw/BeingThrown 구조에 맞춤)
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
        // 멀티히트 제한(히트박스 UID 기반)
        var hitBox = ev.cd.boxA.type == BoxType.Hit ? ev.cd.boxA : ev.cd.boxB;
        int uid = hitBox.uid;
        int atkId = ev.attacker.GetInstanceID();
        int defId = ev.defender.GetInstanceID();

        int cd = ev.skill != null ? ev.skill.rehitCooldownFrames : 0;
        if (_rehitUntil.TryGetValue((atkId, defId, uid), out int next) && _frame < next)
            return; // 아직 재히트 금지
        _rehitUntil[(atkId, defId, uid)] = _frame + Mathf.Max(1, cd);

        // 1) 패리 윈도우 체크
        bool parry = TryParry(ev, out bool justParry);
        if (parry)
        {
            ApplyParry(ev, justParry);
            // 멀티히트 방지 레지스트리에 등록
            MarkHitThisAttack(ev.atkProp.attackInstanceId, ev.defender.GetInstanceID());
            return;
        }

        // 가드 여부: GuardTrigger 접촉 + 피격자가 실제로 가드를 유지(입력) 중일 때
        bool blocked = ev.guardTouch && IsHoldingGuard(ev.defProp, ev.atkProp);

        // 게이지 충전
        ev.atkProp.ChargeDriveGauge(ev.skill.driveGaugeChargeAmount);
        ev.atkProp.ChargeSAGauge(ev.skill.saGaugeChargeAmount);

        // 스턴 & 데미지
        if (blocked)
        {
            // 가드 시
            if (ev.blockstun > 0)
            {
                ev.defProp.SetBlockstun(ev.blockstun);
                ev.defProp.fsm.TransitionTo("GuardHit");
            }
        }
        else
        {
            // 히트 시
            if (ev.damage > 0f)
            {
                int attackerId = (ev.attacker != null) ? ev.attacker.GetInstanceID() : 0;
                float finalDamage = ev.defProp.RegisterComboAndScaleDamage(attackerId, ev.damage);
                ev.defProp.ApplyDamage(finalDamage);
            }
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

            // 히트스탑
            int atkInstance = ev.atkProp.attackInstanceId;
            float damage = ev.damage;

            int frames = CalcHitstopFramesByDamage(damage, false); //TODO: 추후 카운터 여부 처리, 콤보 시 적용 x

            ApplyHitstopOnce(atkInstance, frames);

            if (frames >= 0 && CameraShakeHook.Try(out var sh))
            {
                // 피해량을 0..1로 정규화(예: 120데미지 = 1.0, 60 = 0.5 등)
                float mag = Mathf.Clamp01(damage / 120f);

                // 짧고 굵게
                sh.Impulse(mag, 0.10f, extraSeed: ev.atkProp.attackInstanceId);
            }
        }

        // 수평 push 분배 요청(velocity 사용 안 함)
        EnqueuePush(ev, blocked);

        // 넉다운
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

        // 이 리졸버는 "나(_me)가 관련된 이벤트만" 모으므로,
        // defender가 나일 때에만 분배 큐에 올린다. (상대 캐릭터도 자기 리졸버에서 자체 큐를 가짐)
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
        // Skill_SO에 거리/기간 필드가 없으므로 임시 기본값 사용.
        // 필요 시 skill.knockbackVelocity를 거리로 환산하여 보정.
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

        // 분배 누적 오차를 줄이기 위한 최소 보장
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
    /// 피격자(defender)를 우선 dir 방향으로 밀고, 벽으로 막힌 나머지는 공격자(attacker)를 역방향으로 이동.
    /// 실제 이동(수신자+공격자)의 합을 반환(= 소비된 거리).
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

        // ① 남은 공간 계산을 "실내측 엣지" 기준으로
        float defFree = (dir > 0f)
            ? Mathf.Max(0f, InnerRightX - defAabb.xMax)
            : Mathf.Max(0f, defAabb.xMin - InnerLeftX);

        float defMove = Mathf.Min(desired, defFree);
        if (defMove > 0f)
        {
            defender.Position += new Vector2(dir * defMove, 0f);
            defender.SyncTransform();

            // ② 이동 후 최종 클램프(겹침 방지용)
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

                // 공격자도 클램프
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
        // 안전 가정치
        return new Rect(pe.Position.x - 0.25f, pe.Position.y, 0.5f, 1f);
    }
    #endregion

    #region Guard / KD helpers
    // 공격자 기준 "Back/DownBack" 유지 여부 판단
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

    #region HitStop
    /// <summary> 같은 프레임 내에서 같은 공격(attackInstanceId)에 대해 히트스탑을 1회만 적용 </summary>
    static void ApplyHitstopOnce(int attackInstanceId, int frames)
    {
        // 프레임 바뀌면 세트 초기화
        int f = UnityEngine.Time.frameCount;
        if (f != s_lastAppliedFrame)
        {
            s_lastAppliedFrame = f;
            s_appliedAttackIdsThisFrame.Clear();
        }

        if (frames <= 0) return;
        if (s_appliedAttackIdsThisFrame.Contains(attackInstanceId)) return;

        s_appliedAttackIdsThisFrame.Add(attackInstanceId);

        // TimeController가 프로젝트에 이미 추가돼 있다고 가정
        if (TimeController.Instance != null)
            TimeController.Instance.ApplyHitstop(frames);
    }

    /// <summary>
    /// 피해량 기반 히트스탑 프레임 산출.
    /// - 피격 성공: damage 계수 높게
    /// - 카운터면 +보정 (있으면)
    /// </summary>
    static int CalcHitstopFramesByDamage(float damage, bool isCounter)
    {
        if (damage <= 300) return 0;

        // 튜닝 파라미터 (원하면 ScriptableObject로 빼도 됨)
        const float kHit = 0.5f;   // 히트: 1 데미지당 0.5프레임
        const int minHit = 3;
        const int maxFrames = 14;
        const int counterBonus = 2;

        float raw = damage * kHit;
        int frames = Mathf.RoundToInt(raw);

        frames = Mathf.Clamp(frames, minHit, maxFrames);
        if (isCounter) frames = Mathf.Min(frames + counterBonus, maxFrames);
        
        return Mathf.Max(0, frames);
    }
    #endregion

    static bool AlreadyHitThisAttack(int attackInstanceId, int defenderUid)
    {
        if (s_hitOnceRegistry.TryGetValue(attackInstanceId, out var set))
            return set.Contains(defenderUid);
        return false;
    }

    static void MarkHitThisAttack(int attackInstanceId, int defenderUid)
    {
        if (!s_hitOnceRegistry.TryGetValue(attackInstanceId, out var set))
        {
            set = new System.Collections.Generic.HashSet<int>();
            s_hitOnceRegistry[attackInstanceId] = set;
        }
        set.Add(defenderUid);
    }

    // “패리 가능” 기본 룰: 드라이브패리 상태 또는 패리 윈도우 활성, 투척/잡기는 무시
    private bool TryParry(in FrameEvent ev, out bool justParry)
    {
        justParry = false;
        if (ev.defProp == null || ev.atkProp == null) return false;
        if (ev.skill != null && ev.skill.skillFlag == SkillFlag.DriveReversal) return false; // 예: 역가드류 예외
        if (ev.cd.boxA.type == BoxType.Throw || ev.cd.boxB.type == BoxType.Throw) return false; // 잡기 불가

        // 패리 입력 상태(DriveParryState) or 패리 윈도우 활성
        bool active = (ev.defProp.characterStateTag == CharacterStateTag.DriveParry) || ev.defProp.IsInParry;
        if (!active) return false;

        // 방향/높낮이 제한을 둔다면 여기에: (SF6식으로는 상/하 공용 방어)
        // ex) 하단만 가드 가능한 패리로 제한하려면 ev.cd.hitLevel 비교…

        // 저스트 여부
        justParry = ev.defProp.IsInJustParry;

        return true;
    }

    // 패리 성공 처리
    private void ApplyParry(in FrameEvent ev, bool justParry)
    {
        // 1) 히트스탑: 저스트가 더 큼(공수 모두 멈춤)
        int stop = justParry ? 12 : 8;
        ApplyHitstopOnce(ev.atkProp.attackInstanceId, stop); // TimeController로 공용 적용

        if (justParry)
        {
            // 패리 락 없음
            ev.defProp.ClearParryLock();

            // 가시적으로 ‘즉각’ 체감: 패리 상태에 곧바로 캔슬 허용
            ev.defProp.isSkillCancelable = true;
        }
        else // 일반 패리만 '블록스턴만큼 고정'
        {
            // ev.blockstun 은 위에서 스킬로부터 계산된 값 사용
            ev.defProp.BeginParryLockByBlockstun(ev.blockstun);
        }

            // 2) 데미지/경직/넉백 무효
            // (히트/블록 스턴, 넉다운/런치 등 어떤 것도 적용 안 함)

            // 3) 드라이브 게이지 보상
            float gain = justParry ? 120f : 80f; // 네 UI/게이지 스케일에 맞춰 조절
        ev.defProp.ChargeDriveGauge(gain);

        // 이펙트
        int right = ev.defProp.isFacingRight ? 1 : -1;
        Quaternion rot = Quaternion.identity;
        rot.eulerAngles = new Vector3(right > 0 ? 0f : 180f, 0f, 0f);
        if (FxService.Instance != null) FxService.Instance.Spawn("DriveParryHit", ev.cd.hitPoint, rot);

        //TODO: 성공 사운드

        if (justParry && TimeController.Instance != null)
        {
            TimeController.Instance.ApplySlowMotion(0.2f, 0.8f);
        }
    }
}

public static class CameraShakeHook
{
    public static bool Try(out CameraShake sh)
    {
        sh = GameObject.FindFirstObjectByType<CameraShake>();
        return sh != null;
    }
}