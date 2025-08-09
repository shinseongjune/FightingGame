using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PhysicsEntity))]
public class CollisionResolver : MonoBehaviour, ITicker
{
    private readonly List<CollisionData> frameCollisions = new();
    private PhysicsEntity me;

    // �ܺη� ������ �̺�Ʈ (���ϸ� FSM���� ����)
    public event Action<HitData> OnHitResolved;
    public event Action<PhysicsEntity, PhysicsEntity, CollisionData> OnThrowResolved;
    public event Action<PhysicsEntity, PhysicsEntity, CollisionData> OnGuardResolved;

    private void Awake()
    {
        me = GetComponent<PhysicsEntity>();
    }

    void OnEnable() { BoxManager.Instance.OnCollision += OnCollision; }
    void OnDisable() { BoxManager.Instance.OnCollision -= OnCollision; }

    void OnCollision(CollisionData data)
    {
        // �� �ڽ��� ����� �浹�� ������
        if (data?.boxA?.owner == me || data?.boxB?.owner == me)
            frameCollisions.Add(data);
    }

    public void Tick()
    {
        if (frameCollisions.Count == 0) return;

        // �� ƽ�� �� ĳ���Ϳ� ���� ��Ȯ�� 1�Ǹ� ó��
        CollisionData winner = null;
        int winnerPrio = int.MinValue;

        for (int i = 0; i < frameCollisions.Count; i++)
        {
            var cd = frameCollisions[i];
            if (cd?.boxA == null || cd.boxB == null) continue;

            // �� �ڽ� / ��� �ڽ�
            bool iAmA = cd.boxA.owner == me;
            var myBox = iAmA ? cd.boxA : cd.boxB;
            var otherBox = iAmA ? cd.boxB : cd.boxA;

            // ���� ���� ���� �̺�Ʈ�� ��ŵ(���)
            if (myBox?.owner != me) continue;

            // Ÿ�� ����
            // ��ȿ: (Hit|Throw|GuardTrigger) �� Hurt
            int prio = Priority(myBox, otherBox);
            if (prio <= 0) continue;

            // Ÿ�̺극��Ŀ(���� �켱���� ��): �� ū ��ħ ���� �� �� ���� �߰ߵ� ��
            if (prio > winnerPrio || (prio == winnerPrio && OverlapArea(cd) > OverlapArea(winner)))
            {
                winnerPrio = prio;
                winner = cd;
            }
        }

        if (winner != null)
        {
            // ���� 1�Ǹ� Ȯ�� ó��
            ApplyWinner(winner);
        }

        frameCollisions.Clear();
    }

    private void ResolveHit(CollisionData cd)
    {
        var (atkBox, defBox) = AttackerDefender(cd);
        var hit = new HitData
        {
            collision = cd,
            attacker = atkBox.owner,
            taker = defBox.owner,
            skill = null, // �ʿ��ϸ� ������ ������Ʈ(��: SkillExecutor)���� ä�� �ֱ�
            height = ClassifyHitHeight(defBox, cd.hitPoint),
            direction = ClassifyHitDirection(atkBox.owner, defBox.owner, cd.hitPoint),
        };
        OnHitResolved?.Invoke(hit);
        // TODO: FSM ����/HP����/�˹� �� ����
    }

    private void ResolveThrow(CollisionData cd)
    {
        var (atkBox, defBox) = AttackerDefender(cd);
        OnThrowResolved?.Invoke(atkBox.owner, defBox.owner, cd);
        // TODO: FSM ����(BeingThrown ��) ����
    }

    private void ResolveGuard(CollisionData cd)
    {
        var (atkBox, defBox) = AttackerDefender(cd);
        OnGuardResolved?.Invoke(atkBox.owner, defBox.owner, cd);
        // TODO: ���� ���� ����/���� ���� ���� ��
    }

    // --- ��ƿ ---

    // Hit > Throw > GuardTrigger
    private static int Priority(BoxComponent my, BoxComponent other)
    {
        // �� ���忡��: ���� Hurt�� ����� Hit/Throw/GuardTrigger�� �������� ����ɡ� �̺�Ʈ.
        // ���� Hit/Throw/GuardTrigger�� ������ ���ѡ� �̺�Ʈ�ε�, ���� ƽ�� "���� �ǰ�" �̺�Ʈ�� �����ϸ�
        // ����(BoxManager)���� �̹� ��� �켱������ 1�Ǹ� ���� ���̰�,
        // Ȥ�� �� �� �������� ���⼭ Hit > Throw > Guard�� ������.
        if (IsPair(my, other, BoxType.Hurt, BoxType.Hit)) return 3;
        if (IsPair(my, other, BoxType.Hurt, BoxType.Throw)) return 2;
        if (IsPair(my, other, BoxType.Hurt, BoxType.GuardTrigger)) return 1;

        // ���� ���� ���� �켱������ �����ϰ� �ξ� �� ƽ �ϳ��� ���õǰ� ��
        if (IsPair(my, other, BoxType.Hit, BoxType.Hurt)) return 3;
        if (IsPair(my, other, BoxType.Throw, BoxType.Hurt)) return 2;
        if (IsPair(my, other, BoxType.GuardTrigger, BoxType.Hurt)) return 1;

        return 0;
    }

    private static bool IsPair(BoxComponent a, BoxComponent b, BoxType x, BoxType y)
        => (a.type == x && b.type == y) || (a.type == y && b.type == x);

    private static float OverlapArea(CollisionData cd)
    {
        if (cd == null) return -1f;
        Rect r1 = cd.boxA.GetAABB();
        Rect r2 = cd.boxB.GetAABB();
        float w = Mathf.Max(0, Mathf.Min(r1.xMax, r2.xMax) - Mathf.Max(r1.xMin, r2.xMin));
        float h = Mathf.Max(0, Mathf.Min(r1.yMax, r2.yMax) - Mathf.Max(r1.yMin, r2.yMin));
        return w * h;
    }

    private void ApplyWinner(CollisionData cd)
    {
        var (atkBox, defBox) = AttackerDefender(cd);

        if (IsPair(atkBox, defBox, BoxType.Hit, BoxType.Hurt))
        {
            ResolveHit(cd);
        }
        else if (IsPair(atkBox, defBox, BoxType.Throw, BoxType.Hurt))
        {
            ResolveThrow(cd);
        }
        else if (IsPair(atkBox, defBox, BoxType.GuardTrigger, BoxType.Hurt))
        {
            ResolveGuard(cd);
        }
    }

    private static (BoxComponent attacker, BoxComponent defender) AttackerDefender(CollisionData cd)
    {
        var a = cd.boxA; var b = cd.boxB;
        if (a.type == BoxType.Hurt && b.type != BoxType.Hurt) return (b, a);
        if (b.type == BoxType.Hurt && a.type != BoxType.Hurt) return (a, b);
        return (a, b); // ������
    }

    private static HitHeight ClassifyHitHeight(BoxComponent defenderHurt, Vector2 hitPoint)
    {
        Rect hr = defenderHurt.GetAABB();
        float h = hr.height;
        float lowTop = hr.yMin + h / 3f;
        float midTop = hr.yMin + 2f * h / 3f;

        if (hitPoint.y < lowTop) return HitHeight.Low;
        if (hitPoint.y < midTop) return HitHeight.Middle;
        return HitHeight.High;
    }

    private static HitDirection ClassifyHitDirection(PhysicsEntity attacker, PhysicsEntity defender, Vector2 hitPoint)
    {
        // ���� �ܼ��� ����: ��� �߽� ��� ��/�� + ��/�Ʒ�
        Vector2 d = hitPoint - defender.Position;
        if (Mathf.Abs(d.x) >= Mathf.Abs(d.y))
            return d.x >= 0 ? HitDirection.Right : HitDirection.Left;
        else
            return d.y >= 0 ? HitDirection.Up : HitDirection.Down;
    }
}
