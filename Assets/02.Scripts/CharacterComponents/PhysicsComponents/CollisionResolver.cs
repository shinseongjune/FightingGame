using System.Collections.Generic;
using System.Linq;
using UnityEngine;

enum GuardPosture { Standing, Crouching, Airborne }

class HitSnapshot
{
    public CollisionData cd;
    public PhysicsEntity attacker;
    public PhysicsEntity defender;

    public Skill_SO skill;     // 충돌 '당시' 공격 스킬(스냅샷)
    public int hitstun;        // 당시 확정된 경직/블록스턴 수치
    public int blockstun;

    public float knockback;

    public bool isThrow;
    public bool isGuardTrigger;
}

[RequireComponent(typeof(CharacterFSM))]
[RequireComponent(typeof(CharacterProperty))]
[RequireComponent(typeof(PhysicsEntity))]
public class CollisionResolver : MonoBehaviour, ITicker
{
    PhysicsEntity me;

    // 이번 틱에 “나와 관련된” 충돌만 모아두는 큐
    private readonly List<HitSnapshot> frameEvents = new();

    private BoxManager boxManager;

    // 중복 충돌 방지용
    readonly HashSet<(PhysicsEntity atk, PhysicsEntity def, int inst)> hitOnce = new();

    // 프레임 카운터(내부용; Tick마다++)
    int _frame;

    // (공격자, 피격자, 박스UID) -> nextAllowedFrame
    readonly Dictionary<(int atkId, int defId, int boxUid), int> _rehitUntil = new();

    void Awake()
    {
        me = GetComponent<PhysicsEntity>();
    }

    void OnEnable()
    {
        boxManager = BoxManager.Instance;
        if (boxManager != null)
            boxManager.OnCollision += OnCollision;
    }

    void OnDisable()
    {
        if (boxManager != null)
            boxManager.OnCollision -= OnCollision;

        boxManager = null;
    }

    // BoxManager에서 모든 우선순위 정리 후 페어별로 호출됨
    void OnCollision(CollisionData cd)
    {
        if (cd.boxA == null || cd.boxB == null) return;
        if (cd.boxA.owner != me && cd.boxB.owner != me) return; // 나와 무관하면 무시

        // Throw 우선 스냅샷
        if (IsPair(cd, BoxType.Throw, BoxType.Hurt, out var atkT, out var defT))
        {
            frameEvents.Add(new HitSnapshot
            {
                cd = cd,
                attacker = atkT,
                defender = defT,
                isThrow = true
            });
            return;
        }

        // GuardTrigger 스냅샷 (선가드 유발용)
        if (IsPair(cd, BoxType.GuardTrigger, BoxType.Hurt, out var atkG, out var defG))
        {
            // 공격 스킬 스냅샷: 박스에 sourceSkill이 있으면 우선, 없으면 currentSkill
            var atkBox = cd.boxA.type == BoxType.GuardTrigger ? cd.boxA : cd.boxB;
            var atkProp = atkG.GetComponent<CharacterProperty>();
            var skill = (atkBox is not null ? atkBox.sourceSkill : null) ?? atkProp?.currentSkill;

            frameEvents.Add(new HitSnapshot
            {
                cd = cd,
                attacker = atkG,
                defender = defG,
                skill = skill,
                isGuardTrigger = true
            });
            return;
        }

        // Hit 스냅샷 (경직/넉백은 여기서 확정)
        if (IsPair(cd, BoxType.Hit, BoxType.Hurt, out var atk, out var def))
        {
            var atkProp = atk.GetComponent<CharacterProperty>();

            // ★ 충돌 '당시' 스킬을 박스에서 스냅샷
            var hitBox = cd.boxA.type == BoxType.Hit ? cd.boxA : cd.boxB;
            var skill = (hitBox is not null ? hitBox.sourceSkill : null) ?? atkProp?.currentSkill;
            
            // ★ 중복히트 1회: 공격 인스턴스ID 기준 (스킬 진입마다 증가)
            int inst = atkProp != null ? atkProp.attackInstanceId : 0;
            var key = (atk: atk, def: def, inst: inst);
            if (hitOnce.Contains(key)) return;   // 이미 같은 인스턴스로 이 상대를 맞췄으면 무시
            hitOnce.Add(key);

            int hitstun = skill != null ? skill.hitstunDuration : 12;
            int blockstun = skill != null ? skill.blockstunDuration : 10;
            float knockback = skill != null ? skill.knockbackDistance : 1;

            frameEvents.Add(new HitSnapshot
            {
                cd = cd,
                attacker = atk,
                defender = def,
                skill = skill,
                hitstun = hitstun,
                blockstun = blockstun,
                knockback = knockback
            });
        }
    }

    public void Tick()
    {
        _frame++;

        if (frameEvents.Count == 0) return;

        // 1) 선가드 유발(GuardTrigger) 먼저 처리
        for (int i = 0; i < frameEvents.Count; i++)
        {
            var ev = frameEvents[i];
            if (!ev.isGuardTrigger) continue;

            // 내가 수비자일 때만 선가드 적용
            if (ev.defender == me && IsHoldingGuard(ev.defender))
            {
                EnsureGuarding(ev.defender);
            }
        }

        // 2) Throw/Hit 처리
        for (int i = 0; i < frameEvents.Count; i++)
        {
            var ev = frameEvents[i];

            // ---- Throw ----
            if (ev.isThrow)
            {
                if (ev.defender == me)
                {
                    // 잡힌 쪽
                    var defFSM = ev.defender.GetComponent<CharacterFSM>();
                    defFSM?.TransitionTo("BeingThrown");
                    (defFSM?.Current as BeingThrownState)?.SetTrower(ev.attacker.property);
                }
                else if (ev.attacker == me)
                {
                    // 시전자 쪽
                    var atFSM = ev.attacker.GetComponent<CharacterFSM>();
                    atFSM?.TransitionTo("Throw");
                    (atFSM?.Current as ThrowState)?.SetTarget(ev.defender);
                }
                continue;
            }

            var hitBox = ev.cd.boxA.type == BoxType.Hit ? ev.cd.boxA : ev.cd.boxB;
            int cd = ev.skill.rehitCooldownFrames;

            int atkId = ev.attacker.GetInstanceID();
            int defId = ev.defender.GetInstanceID();
            int uid = hitBox.uid;

            // 쿨다운 중이면 스킵
            if (_rehitUntil.TryGetValue((atkId, defId, uid), out int next) && _frame < next)
            {
                continue;
            }

            // 다음 허용 프레임 갱신
            _rehitUntil[(atkId, defId, uid)] = _frame + cd;

            // ---- Hit / Guard ----
            // 가드 가능 여부 계산(당시 스킬의 hitLevel 사용)
            var level = ev.skill != null ? ev.skill.hitLevel : HitLevel.Mid;
            var posture = GetPosture(ev.defender);
            bool holdingBack = IsHoldingBack(ev.defender, ev.attacker);
            bool guardAllowed = holdingBack && CanBlock(level, posture);

            if (ev.defender == me)
            {
                if (guardAllowed)
                {
                    // ★ 스냅샷의 blockstun 사용 (currentSkill 의존 X)
                    var defProp = ev.defender.GetComponent<CharacterProperty>();
                    defProp?.SetBlockstun(ev.blockstun);

                    var defFSM = ev.defender.GetComponent<CharacterFSM>();
                    defFSM?.Current?.HandleGuard(ev.attacker, ev.defender, ev.cd);
                    defFSM?.TransitionTo("GuardHit");
                }
                else
                {
                    var defProp = ev.defender.GetComponent<CharacterProperty>();
                    var atkProp = ev.attacker.GetComponent<CharacterProperty>();

                    // 넉백은 공격자 페이싱 기준 + 스냅샷 스킬의 성질 사용
                    float dir = (atkProp != null && atkProp.isFacingRight) ? +1f : -1f;
                    Vector2 kb = (ev.skill != null && ev.skill.causesLaunch)
                                    ? new Vector2(ev.knockback * dir, 8f)
                                    : new Vector2(ev.knockback * dir, 0f);

                    // ★ 스냅샷의 hitstun 사용
                    defProp?.SetHitstun(ev.hitstun, kb);

                    var defFSM = ev.defender.GetComponent<CharacterFSM>();
                    if (ev.skill != null && ev.skill.causesKnockdown)
                    {
                        defFSM?.TransitionTo("Knockdown");
                    }
                    else
                    {
                        if (ev.defender.isGrounded) defFSM?.TransitionTo("HitGround");
                        else defFSM?.TransitionTo("HitAir");
                    }

                    // 수신자 상태 훅
                    defFSM?.Current?.HandleHit(MakeHitData(ev.attacker, ev.defender, ev.cd));

                    if (ev.skill != null) defProp.hp = Mathf.Max(0, defProp.hp - ev.skill.damageOnHit);
                }
            }

            if (ev.attacker == me)
            {
                var atkFSM = ev.attacker.GetComponent<CharacterFSM>();
                var atkState = atkFSM?.Current;
                if (guardAllowed) atkState?.HandleGuard(ev.attacker, ev.defender, ev.cd);
                else atkState?.HandleHit(MakeHitData(ev.attacker, ev.defender, ev.cd));
            }
        }

        // 3) 프레임 이벤트 비움
        frameEvents.Clear();
    }

    // ---------- 유틸 ----------

    private static bool IsPair(CollisionData cd, BoxType x, BoxType y, out PhysicsEntity atk, out PhysicsEntity def)
    {
        atk = def = null;
        var a = cd.boxA; var b = cd.boxB;
        if (a == null || b == null) return false;

        if ((a.type == x && b.type == y) || (a.type == y && b.type == x))
        {
            // 공격자 = (Hit/Throw/GuardTrigger)쪽, 피격자 = Hurt쪽
            BoxComponent atkBox = a.type == x ? a : b;
            BoxComponent defBox = a.type == y ? a : b;
            atk = atkBox.owner;
            def = defBox.owner;
            return true;
        }
        return false;
    }

    private bool IsHoldingGuard(PhysicsEntity def)
    {
        // 단순판정: 입력 버퍼의 마지막 방향이 Back/DownBack
        var ib = def.GetComponent<InputBuffer>();
        var d = ib != null ? ib.LastInput.direction : Direction.Neutral;
        return d == Direction.Back || d == Direction.DownBack;
    }

    private bool IsCurrentlyGuarding(PhysicsEntity def)
    {
        var f = def.GetComponent<CharacterFSM>();
        var s = f?.Current?.StateTag;
        return s.HasValue && s.Value == CharacterStateTag.Guarding;
    }

    private void EnsureGuarding(PhysicsEntity def)
    {
        var f = def.GetComponent<CharacterFSM>();
        if (!IsCurrentlyGuarding(def))
            f?.TransitionTo("Guarding");
    }

    private HitData MakeHitData(PhysicsEntity atk, PhysicsEntity def, CollisionData cd)
    {
        var atkProp = atk.GetComponent<CharacterProperty>();
        var skill = atkProp != null ? atkProp.currentSkill : null;

        return new HitData
        {
            attacker = atk,
            taker = def,
            collision = cd,
            skill = skill,
            // 높이/방향은 필요시 더 정교하게
            height = HitHeight.Middle,
            direction = atkProp != null && atkProp.isFacingRight ? HitDirection.Right : HitDirection.Left
        };
    }

    // -------- 가드 ---------

    GuardPosture GetPosture(PhysicsEntity def)
    {
        if (!def.isGrounded) return GuardPosture.Airborne;

        var fsm = def.GetComponent<CharacterFSM>();
        var tag = fsm?.Current?.StateTag;

        // 상태 태그나 입력 둘 중 하나만 내려도 인식되게
        if (tag == CharacterStateTag.Crouch) return GuardPosture.Crouching;

        var ib = def.GetComponent<InputBuffer>();
        var d = ib != null ? ib.LastInput.direction : Direction.Neutral;
        if (d is Direction.Down or Direction.DownBack or Direction.DownForward)
            return GuardPosture.Crouching;

        return GuardPosture.Standing;
    }

    // 2) 가드 입력(뒤/뒤하) 유지 여부
    bool IsHoldingBack(PhysicsEntity def, PhysicsEntity atk)
    {
        var ib = def.GetComponent<InputBuffer>();
        var d = ib != null ? ib.LastInput.direction : Direction.Neutral;

        // 단순화: 공격자가 내 오른쪽에 있으면 Back=Left, 왼쪽에 있으면 Back=Right 로 치환해도 됨.
        // 우선은 좌우 뒤집기 없이 Back/DownBack만 체크(원하면 교차 판정 추가 가능)
        return d == Direction.Back || d == Direction.DownBack;
    }

    // 3) 가드 가능 테이블(요구사항 반영)
    bool CanBlock(HitLevel level, GuardPosture posture)
    {
        switch (posture)
        {
            case GuardPosture.Standing:
                return level == HitLevel.High || level == HitLevel.Mid || level == HitLevel.Overhead;
            case GuardPosture.Crouching:
                return level == HitLevel.Low;
            default: // Airborne
                return false; // 공중가드 없음
        }
    }
}
