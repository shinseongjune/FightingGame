using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PhysicsEntity))]
public class BoxPresetApplier : MonoBehaviour, ITicker
{
    [SerializeField] Transform boxRoot; // �ڽ� ������Ʈ�� ���� �θ� (������ this.transform)
    private PhysicsEntity owner;
    private AnimationPlayer anim;

    private Skill_SO activeSkill;
    private readonly List<BoxComponent> currentBoxes = new(); // ���� ����ִ� �ڽ���

    // ���� Ǯ: Ÿ�Ժ��� ����
    private readonly Dictionary<BoxType, Stack<BoxComponent>> pool = new();

    void Awake()
    {
        owner = GetComponent<PhysicsEntity>();
        anim = GetComponent<AnimationPlayer>();
        if (boxRoot == null) boxRoot = transform;
        foreach (BoxType t in System.Enum.GetValues(typeof(BoxType)))
            pool[t] = new Stack<BoxComponent>();
    }

    public void ApplySkill(Skill_SO skill)
    {
        activeSkill = skill;
        // ��ų ���� ������ �ϴ� �����ϰ�
        ClearAll();
        // �ʿ��ϸ� ����� �����Ǵ� �ڽ�(startFrame == 0)���� �� �� �̸� ��Ƶ� �� (����)
        TrySyncBoxes();
    }

    public void Tick()
    {
        if (activeSkill == null) return;
        TrySyncBoxes();
    }

    public void ClearAll()
    {
        for (int i = currentBoxes.Count - 1; i >= 0; i--)
            Despawn(currentBoxes[i]);
        currentBoxes.Clear();
        activeSkill = null; // ���� ���� �� ��ų ����
    }

    private void TrySyncBoxes()
    {
        int f = anim != null ? anim.CurrentFrame : 0;
        if (activeSkill == null) return;

        // 1) �̹� �����ӿ� �����Ǿ�� �ϴ� Ÿ�� ��Ʈ �����
        var wanted = ListStatic<BoxKey>.Rent();
        for (int i = 0; i < activeSkill.boxLifetimes.Count; i++)
        {
            var life = activeSkill.boxLifetimes[i];
            if (f < life.startFrame || f > life.endFrame) continue;

            wanted.Add(new BoxKey
            {
                type = life.type,
                center = life.box.center,
                size = life.box.size
            });
        }

        // 2) ���� �� �� �� �ʿ� ���� �� �ݳ�
        for (int i = currentBoxes.Count - 1; i >= 0; i--)
        {
            var b = currentBoxes[i];
            var k = new BoxKey { type = b.type, center = b.offset, size = b.size };
            if (!wanted.Contains(k))
            {
                Despawn(b);
                currentBoxes.RemoveAt(i);
            }
        }

        // 3) �ʿ��ѵ� ���� �� ����/����
        foreach (var k in wanted)
        {
            if (!HasBox(k))
                currentBoxes.Add(Spawn(k));
        }

        ListStatic<BoxKey>.Return(wanted);
    }

    private bool HasBox(BoxKey k)
    {
        for (int i = 0; i < currentBoxes.Count; i++)
        {
            var b = currentBoxes[i];
            if (b.type == k.type && b.offset == k.center && b.size == k.size)
                return true;
        }
        return false;
    }

    private BoxComponent Spawn(BoxKey k)
    {
        BoxComponent box;
        if (pool[k.type].Count > 0) box = pool[k.type].Pop();
        else
        {
            var go = new GameObject($"{k.type}Box");
            go.transform.SetParent(boxRoot, false);
            box = go.AddComponent<BoxComponent>();
        }

        box.type = k.type;
        box.owner = owner;
        box.offset = k.center;
        box.size = k.size;
        box.gameObject.SetActive(true);

        // BoxManager�� ���
        BoxManager.Instance.Register(box);
        return box;
    }

    private void Despawn(BoxComponent box)
    {
        if (box == null) return;
        BoxManager.Instance.Unregister(box);
        box.gameObject.SetActive(false);
        pool[box.type].Push(box);
    }

    struct BoxKey
    {
        public BoxType type;
        public Vector2 center;
        public Vector2 size;
    }
}

// ������ �ӽ� ����Ʈ Ǯ (GC ���̱�� ���û���)
static class ListStatic<T>
{
    static readonly Stack<List<T>> s = new();
    public static List<T> Rent() => s.Count > 0 ? s.Pop() : new List<T>(8);
    public static void Return(List<T> list) { list.Clear(); s.Push(list); }
}
