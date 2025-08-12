using System.Collections.Generic;
using UnityEngine;

enum GuardPosture { Standing, Crouching, Airborne }

[RequireComponent(typeof(CharacterFSM))]
[RequireComponent(typeof(CharacterProperty))]
[RequireComponent(typeof(PhysicsEntity))]
public class CollisionResolver : MonoBehaviour, ITicker
{
    CharacterFSM fsm;
    CharacterProperty prop;
    PhysicsEntity me;
    InputBuffer input;

    // 이번 틱에 “나와 관련된” 충돌만 모아두는 큐
    readonly List<CollisionData> frameCollisions = new();

    private BoxManager boxManager;

    void Awake()
    {
        fsm = GetComponent<CharacterFSM>();
        prop = GetComponent<CharacterProperty>();
        me = GetComponent<PhysicsEntity>();
        input = GetComponent<InputBuffer>();
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
        // 나와 관련 없으면 무시
        if (cd.boxA == null || cd.boxB == null) return;
        if (cd.boxA.owner != me && cd.boxB.owner != me) return;

        frameCollisions.Add(cd);
    }

    public void Tick()
    {
        if (frameCollisions.Count == 0) { return; }

        // 1) 먼저 GuardTrigger를 스캔해서 가드 선입력 유발
        for (int i = 0; i < frameCollisions.Count; i++)
        {
            var cd = frameCollisions[i];
            if (IsPair(cd, BoxType.GuardTrigger, BoxType.Hurt, out var atk, out var def))
            {
                if (def == me && IsHoldingGuard(def))
                {
                    EnsureGuarding(def);
                }
            }
        }

        // 2) Hit / Throw 처리
        for (int i = 0; i < frameCollisions.Count; i++)
        {
            var cd = frameCollisions[i];

            if (IsPair(cd, BoxType.Throw, BoxType.Hurt, out var atk, out var def))
            {
                if (def == me)
                {
                    // 피격자 쪽 처리
                    // 피격자 FSM은 BeingThrown으로
                    var defFSM = def.GetComponent<CharacterFSM>();
                    defFSM?.TransitionTo("BeingThrown");
                }
                else if (atk == me)
                {
                    // 시전자 FSM은 ThrowState로 가고 타깃 연결
                    var atFSM = atk.GetComponent<CharacterFSM>();
                    if (atFSM != null)
                    {
                        // 등록된 풀에서 얻거나 캐스팅
                        var st = atFSM.Current as ThrowState ?? null;
                        atFSM.TransitionTo("Throw");
                        // ThrowState에 대상 세팅
                        var ts = atFSM.Current as ThrowState;
                        ts?.SetTarget(def);
                    }
                }
                continue;
            }

            if (IsPair(cd, BoxType.Hit, BoxType.Hurt, out var attacker, out var defender))
            {
                var atkProp = attacker.GetComponent<CharacterProperty>();
                var skill = atkProp?.currentSkill;
                var level = skill != null ? skill.hitLevel : HitLevel.Mid;

                var posture = GetPosture(defender);
                bool holdingBack = IsHoldingBack(defender, attacker);

                bool guardAllowed = holdingBack && CanBlock(level, posture);

                if (defender == me)
                {
                    if (guardAllowed)
                        ApplyBlockstun(attacker, defender, cd);
                    else
                        ApplyHitstun(attacker, defender, cd);
                }

                if (attacker == me)
                {
                    var atkFSM = attacker.GetComponent<CharacterFSM>();
                    var atkState = atkFSM?.Current;
                    if (guardAllowed) atkState?.HandleGuard(attacker, defender, cd);
                    else atkState?.HandleHit(MakeHitData(attacker, defender, cd));
                }
            }
        }

        frameCollisions.Clear();
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

    private void ApplyBlockstun(PhysicsEntity atk, PhysicsEntity def, CollisionData cd)
    {
        var defProp = def.GetComponent<CharacterProperty>();
        var atkProp = atk.GetComponent<CharacterProperty>();
        var skill = atkProp?.currentSkill;

        int blockstun = skill != null ? skill.blockstunDuration : 10;
        defProp?.SetBlockstun(blockstun);

        // 수신자 쪽 상태로 알림
        var defFSM = def.GetComponent<CharacterFSM>();
        defFSM?.Current?.HandleGuard(atk, def, cd);
        defFSM?.TransitionTo("GuardHit");
    }

    private void ApplyHitstun(PhysicsEntity atk, PhysicsEntity def, CollisionData cd)
    {
        var defProp = def.GetComponent<CharacterProperty>();
        var atkProp = atk.GetComponent<CharacterProperty>();
        var skill = atkProp?.currentSkill;

        int hitstun = skill != null ? skill.hitstunDuration : 12;

        // 넉백 간단 규칙: 발동자 바라보는 방향 기준
        float dir = atkProp != null && atkProp.isFacingRight ? +1f : -1f;
        Vector2 kb = skill != null && skill.causesLaunch
            ? new Vector2(4f * dir, 8f)        // 공중 띄우기
            : new Vector2(6f * dir, 0f);       // 지상 밀기

        defProp?.SetHitstun(hitstun, kb);

        // 다운 유발이면 상태 전이까지 처리(공중/지상 분기)
        if (skill != null && skill.causesKnockdown)
        {
            var defFSM = def.GetComponent<CharacterFSM>();
            defFSM?.TransitionTo("Knockdown"); // 혹은 HardKnockdown 규칙
        }

        // 수신자 현재 상태에도 훅
        var defState = def.GetComponent<CharacterFSM>()?.Current;
        defState?.HandleHit(MakeHitData(atk, def, cd));
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
