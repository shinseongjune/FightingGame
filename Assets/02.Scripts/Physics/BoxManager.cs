using System;
using System.Collections.Generic;
using UnityEngine;

public class BoxManager : Singleton<BoxManager>, ITicker
{
    public List<BoxComponent> activeBoxes = new();

    // ���� �ؼ� �Ŀ��� ����Ǵ� �̺�Ʈ(�� ������ ����ڵ�)
    public event Action<CollisionData> OnCollision;

    public void Register(BoxComponent box) => activeBoxes.Add(box);
    public void Unregister(BoxComponent box) => activeBoxes.Remove(box);

    public void Tick()
    {
        // 1) ��ȿ �浹 ����
        var pending = new List<PendingCollision>();

        int n = activeBoxes.Count;
        for (int i = 0; i < n; i++)
        {
            var a = activeBoxes[i];
            if (!IsActive(a)) continue;

            for (int j = i + 1; j < n; j++)
            {
                var b = activeBoxes[j];
                if (!IsActive(b)) continue;

                if (!AreOpponents(a, b)) continue;

                PairKind kind;
                BoxComponent atk, def;
                if (!Classify(a, b, out kind, out atk, out def)) continue;

                if (!AABBCheck(a, b)) continue;

                var cd = new CollisionData
                {
                    boxA = a,
                    boxB = b,
                    hitPoint = OverlapCenter(a, b),
                };

                pending.Add(new PendingCollision
                {
                    data = cd,
                    kind = kind,
                    priority = PriorityOf(kind),
                    attacker = atk,
                    defender = def
                });
            }
        }

        if (pending.Count == 0) return;

        // 2) (ownerA, ownerB) ������ ���� �׷���
        var grouped = new Dictionary<(PhysicsEntity, PhysicsEntity), List<PendingCollision>>();
        foreach (var p in pending)
        {
            var key = MakePairKey(p.attacker.owner, p.defender.owner);
            if (!grouped.TryGetValue(key, out var list))
            {
                list = new List<PendingCollision>();
                grouped[key] = list;
            }
            list.Add(p);
        }

        // 3) �׷캰 �켱���� ���� �� ����
        foreach (var kv in grouped)
        {
            var list = kv.Value;

            // �ֻ��� �켱���� ã�� (Hit > Throw > GuardTrigger)
            int best = int.MinValue;
            foreach (var p in list)
                if (p.priority > best) best = p.priority;

            // ���� �ֿ켱���� �ĺ� �߸�
            var finalists = list.FindAll(p => p.priority == best);

            // --- ��å ---
            // * Hit�� ���ʿ��� ���ÿ� ������ 'Ʈ���̵�' ����ϰ� �� �� �����ص� ��.
            //   �ʿ信 ���� �ϳ��� �����Ϸ��� �Ʒ����� Ÿ�̺극��Ŀ�� ����.
            // * Throw/GuardTrigger�� ��ȣ ��Ÿ��. �ֿ켱�� Hit��� Throw/Guard�� ������.
            // ----------------

            // (��) Ʈ���̵� ���: �ֿ켱 �ĺ� ���� ����
            foreach (var p in finalists)
                OnCollision?.Invoke(p.data);

            // (��) Ʈ���̵� ������ �ٲٰ� �ʹٸ� ���⼭ �ϳ��� ��� Invoke �ϸ� ��.
        }
    }

    // ----- ���� ����� -----

    enum PairKind { Hit, Throw, GuardTrigger }

    class PendingCollision
    {
        public CollisionData data;
        public PairKind kind;
        public int priority;
        public BoxComponent attacker;
        public BoxComponent defender;
    }

    static bool IsActive(BoxComponent b)
        => b != null && b.gameObject.activeSelf && b.owner != null && b.owner.collisionsEnabled;

    static bool AreOpponents(BoxComponent a, BoxComponent b)
        => a.owner != null && b.owner != null && a.owner != b.owner;

    static bool Classify(BoxComponent a, BoxComponent b, out PairKind kind, out BoxComponent attacker, out BoxComponent defender)
    {
        // (Hit|Throw|Guard) �� Hurt �� ��ȿ
        if (IsTypePair(a, b, BoxType.Hit, BoxType.Hurt))
        {
            kind = PairKind.Hit;
            (attacker, defender) = a.type == BoxType.Hit ? (a, b) : (b, a);
            return true;
        }
        if (IsTypePair(a, b, BoxType.Throw, BoxType.Hurt))
        {
            kind = PairKind.Throw;
            (attacker, defender) = a.type == BoxType.Throw ? (a, b) : (b, a);
            return true;
        }
        if (IsTypePair(a, b, BoxType.GuardTrigger, BoxType.Hurt))
        {
            kind = PairKind.GuardTrigger;
            (attacker, defender) = a.type == BoxType.GuardTrigger ? (a, b) : (b, a);
            return true;
        }

        kind = default;
        attacker = defender = null;
        return false;
    }

    static bool IsTypePair(BoxComponent a, BoxComponent b, BoxType x, BoxType y)
        => (a.type == x && b.type == y) || (a.type == y && b.type == x);

    static int PriorityOf(PairKind k)
        => k switch { PairKind.Hit => 3, PairKind.Throw => 2, _ => 1 };

    static (PhysicsEntity, PhysicsEntity) MakePairKey(PhysicsEntity a, PhysicsEntity b)
        => a.GetInstanceID() < b.GetInstanceID() ? (a, b) : (b, a);

    static bool AABBCheck(BoxComponent a, BoxComponent b)
        => a.GetAABB().Overlaps(b.GetAABB());

    static Vector2 OverlapCenter(BoxComponent a, BoxComponent b)
    {
        Rect r1 = a.GetAABB();
        Rect r2 = b.GetAABB();
        float minX = Mathf.Max(r1.xMin, r2.xMin);
        float maxX = Mathf.Min(r1.xMax, r2.xMax);
        float minY = Mathf.Max(r1.yMin, r2.yMin);
        float maxY = Mathf.Min(r1.yMax, r2.yMax);
        if (minX <= maxX && minY <= maxY)
            return new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
        // �̷л� ���� X(Overlaps Ȯ�� ��). ������
        return (r1.center + r2.center) * 0.5f;
    }
}
