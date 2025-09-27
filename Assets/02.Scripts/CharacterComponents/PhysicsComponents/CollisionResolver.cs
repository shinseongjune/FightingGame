using System.Collections.Generic;
using System.Linq;
using UnityEngine;

enum GuardPosture { Standing, Crouching, Airborne }

class HitSnapshot
{
    public CollisionData cd;
    public PhysicsEntity attacker;
    public PhysicsEntity defender;

    public Skill_SO skill;     // �浹 '���' ���� ��ų(������)
    public int hitstun;        // ��� Ȯ���� ����/��Ͻ��� ��ġ
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

    // �̹� ƽ�� ������ ���õȡ� �浹�� ��Ƶδ� ť
    private readonly List<HitSnapshot> frameEvents = new();

    private BoxManager boxManager;

    // �ߺ� �浹 ������
    readonly HashSet<(PhysicsEntity atk, PhysicsEntity def, int inst)> hitOnce = new();

    // ������ ī����(���ο�; Tick����++)
    int _frame;

    // (������, �ǰ���, �ڽ�UID) -> nextAllowedFrame
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

    // BoxManager���� ��� �켱���� ���� �� ���� ȣ���
    void OnCollision(CollisionData cd)
    {
        if (cd.boxA == null || cd.boxB == null) return;
        if (cd.boxA.owner != me && cd.boxB.owner != me) return; // ���� �����ϸ� ����

        // Throw �켱 ������
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

        // GuardTrigger ������ (������ ���߿�)
        if (IsPair(cd, BoxType.GuardTrigger, BoxType.Hurt, out var atkG, out var defG))
        {
            // ���� ��ų ������: �ڽ��� sourceSkill�� ������ �켱, ������ currentSkill
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

        // Hit ������ (����/�˹��� ���⼭ Ȯ��)
        if (IsPair(cd, BoxType.Hit, BoxType.Hurt, out var atk, out var def))
        {
            var atkProp = atk.GetComponent<CharacterProperty>();

            // �� �浹 '���' ��ų�� �ڽ����� ������
            var hitBox = cd.boxA.type == BoxType.Hit ? cd.boxA : cd.boxB;
            var skill = (hitBox is not null ? hitBox.sourceSkill : null) ?? atkProp?.currentSkill;
            
            // �� �ߺ���Ʈ 1ȸ: ���� �ν��Ͻ�ID ���� (��ų ���Ը��� ����)
            int inst = atkProp != null ? atkProp.attackInstanceId : 0;
            var key = (atk: atk, def: def, inst: inst);
            if (hitOnce.Contains(key)) return;   // �̹� ���� �ν��Ͻ��� �� ��븦 �������� ����
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

        // 1) ������ ����(GuardTrigger) ���� ó��
        for (int i = 0; i < frameEvents.Count; i++)
        {
            var ev = frameEvents[i];
            if (!ev.isGuardTrigger) continue;

            // ���� �������� ���� ������ ����
            if (ev.defender == me && IsHoldingGuard(ev.defender))
            {
                EnsureGuarding(ev.defender);
            }
        }

        // 2) Throw/Hit ó��
        for (int i = 0; i < frameEvents.Count; i++)
        {
            var ev = frameEvents[i];

            // ---- Throw ----
            if (ev.isThrow)
            {
                if (ev.defender == me)
                {
                    // ���� ��
                    var defFSM = ev.defender.GetComponent<CharacterFSM>();
                    defFSM?.TransitionTo("BeingThrown");
                    (defFSM?.Current as BeingThrownState)?.SetTrower(ev.attacker.property);
                }
                else if (ev.attacker == me)
                {
                    // ������ ��
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

            // ��ٿ� ���̸� ��ŵ
            if (_rehitUntil.TryGetValue((atkId, defId, uid), out int next) && _frame < next)
            {
                continue;
            }

            // ���� ��� ������ ����
            _rehitUntil[(atkId, defId, uid)] = _frame + cd;

            // ---- Hit / Guard ----
            // ���� ���� ���� ���(��� ��ų�� hitLevel ���)
            var level = ev.skill != null ? ev.skill.hitLevel : HitLevel.Mid;
            var posture = GetPosture(ev.defender);
            bool holdingBack = IsHoldingBack(ev.defender, ev.attacker);
            bool guardAllowed = holdingBack && CanBlock(level, posture);

            if (ev.defender == me)
            {
                if (guardAllowed)
                {
                    // �� �������� blockstun ��� (currentSkill ���� X)
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

                    // �˹��� ������ ���̽� ���� + ������ ��ų�� ���� ���
                    float dir = (atkProp != null && atkProp.isFacingRight) ? +1f : -1f;
                    Vector2 kb = (ev.skill != null && ev.skill.causesLaunch)
                                    ? new Vector2(ev.knockback * dir, 8f)
                                    : new Vector2(ev.knockback * dir, 0f);

                    // �� �������� hitstun ���
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

                    // ������ ���� ��
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

        // 3) ������ �̺�Ʈ ���
        frameEvents.Clear();
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
