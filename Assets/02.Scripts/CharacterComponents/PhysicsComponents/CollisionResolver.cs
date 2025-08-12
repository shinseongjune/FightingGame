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

    // �̹� ƽ�� ������ ���õȡ� �浹�� ��Ƶδ� ť
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

    // BoxManager���� ��� �켱���� ���� �� ���� ȣ���
    void OnCollision(CollisionData cd)
    {
        // ���� ���� ������ ����
        if (cd.boxA == null || cd.boxB == null) return;
        if (cd.boxA.owner != me && cd.boxB.owner != me) return;

        frameCollisions.Add(cd);
    }

    public void Tick()
    {
        if (frameCollisions.Count == 0) { return; }

        // 1) ���� GuardTrigger�� ��ĵ�ؼ� ���� ���Է� ����
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

        // 2) Hit / Throw ó��
        for (int i = 0; i < frameCollisions.Count; i++)
        {
            var cd = frameCollisions[i];

            if (IsPair(cd, BoxType.Throw, BoxType.Hurt, out var atk, out var def))
            {
                if (def == me)
                {
                    // �ǰ��� �� ó��
                    // �ǰ��� FSM�� BeingThrown����
                    var defFSM = def.GetComponent<CharacterFSM>();
                    defFSM?.TransitionTo("BeingThrown");
                }
                else if (atk == me)
                {
                    // ������ FSM�� ThrowState�� ���� Ÿ�� ����
                    var atFSM = atk.GetComponent<CharacterFSM>();
                    if (atFSM != null)
                    {
                        // ��ϵ� Ǯ���� ��ų� ĳ����
                        var st = atFSM.Current as ThrowState ?? null;
                        atFSM.TransitionTo("Throw");
                        // ThrowState�� ��� ����
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

    // ---------- ��ƿ ----------

    private static bool IsPair(CollisionData cd, BoxType x, BoxType y, out PhysicsEntity atk, out PhysicsEntity def)
    {
        atk = def = null;
        var a = cd.boxA; var b = cd.boxB;
        if (a == null || b == null) return false;

        if ((a.type == x && b.type == y) || (a.type == y && b.type == x))
        {
            // ������ = (Hit/Throw/GuardTrigger)��, �ǰ��� = Hurt��
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
        // �ܼ�����: �Է� ������ ������ ������ Back/DownBack
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

        // ������ �� ���·� �˸�
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

        // �˹� ���� ��Ģ: �ߵ��� �ٶ󺸴� ���� ����
        float dir = atkProp != null && atkProp.isFacingRight ? +1f : -1f;
        Vector2 kb = skill != null && skill.causesLaunch
            ? new Vector2(4f * dir, 8f)        // ���� ����
            : new Vector2(6f * dir, 0f);       // ���� �б�

        defProp?.SetHitstun(hitstun, kb);

        // �ٿ� �����̸� ���� ���̱��� ó��(����/���� �б�)
        if (skill != null && skill.causesKnockdown)
        {
            var defFSM = def.GetComponent<CharacterFSM>();
            defFSM?.TransitionTo("Knockdown"); // Ȥ�� HardKnockdown ��Ģ
        }

        // ������ ���� ���¿��� ��
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
            // ����/������ �ʿ�� �� �����ϰ�
            height = HitHeight.Middle,
            direction = atkProp != null && atkProp.isFacingRight ? HitDirection.Right : HitDirection.Left
        };
    }

    // -------- ���� ---------

    GuardPosture GetPosture(PhysicsEntity def)
    {
        if (!def.isGrounded) return GuardPosture.Airborne;

        var fsm = def.GetComponent<CharacterFSM>();
        var tag = fsm?.Current?.StateTag;

        // ���� �±׳� �Է� �� �� �ϳ��� ������ �νĵǰ�
        if (tag == CharacterStateTag.Crouch) return GuardPosture.Crouching;

        var ib = def.GetComponent<InputBuffer>();
        var d = ib != null ? ib.LastInput.direction : Direction.Neutral;
        if (d is Direction.Down or Direction.DownBack or Direction.DownForward)
            return GuardPosture.Crouching;

        return GuardPosture.Standing;
    }

    // 2) ���� �Է�(��/����) ���� ����
    bool IsHoldingBack(PhysicsEntity def, PhysicsEntity atk)
    {
        var ib = def.GetComponent<InputBuffer>();
        var d = ib != null ? ib.LastInput.direction : Direction.Neutral;

        // �ܼ�ȭ: �����ڰ� �� �����ʿ� ������ Back=Left, ���ʿ� ������ Back=Right �� ġȯ�ص� ��.
        // �켱�� �¿� ������ ���� Back/DownBack�� üũ(���ϸ� ���� ���� �߰� ����)
        return d == Direction.Back || d == Direction.DownBack;
    }

    // 3) ���� ���� ���̺�(�䱸���� �ݿ�)
    bool CanBlock(HitLevel level, GuardPosture posture)
    {
        switch (posture)
        {
            case GuardPosture.Standing:
                return level == HitLevel.High || level == HitLevel.Mid || level == HitLevel.Overhead;
            case GuardPosture.Crouching:
                return level == HitLevel.Low;
            default: // Airborne
                return false; // ���߰��� ����
        }
    }
}
