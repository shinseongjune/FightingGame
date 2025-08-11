using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PhysicsEntity))]
public class BoxPresetApplier : MonoBehaviour, ITicker
{
    [SerializeField] Transform boxRoot; // 박스 오브젝트를 붙일 부모 (없으면 this.transform)
    private PhysicsEntity owner;
    private AnimationPlayer anim;

    private Skill_SO activeSkill;
    private readonly List<BoxComponent> currentBoxes = new(); // 지금 살아있는 박스들

    // 간단 풀: 타입별로 재사용
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
        // 스킬 시작 시점엔 일단 깨끗하게
        ClearAll();
        // 필요하면 “즉시 생성되는 박스(startFrame == 0)”를 한 번 미리 깔아도 됨 (선택)
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
        activeSkill = null; // 상태 전이 시 스킬 종료
    }

    private void TrySyncBoxes()
    {
        int f = anim != null ? anim.CurrentFrame : 0;
        if (activeSkill == null) return;

        // 1) 이번 프레임에 유지되어야 하는 타겟 세트 만들기
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

        // 2) 현재 깔린 것 중 필요 없는 건 반납
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

        // 3) 필요한데 없는 건 생성/재사용
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

        // BoxManager에 등록
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

// 간단한 임시 리스트 풀 (GC 줄이기용 선택사항)
static class ListStatic<T>
{
    static readonly Stack<List<T>> s = new();
    public static List<T> Rent() => s.Count > 0 ? s.Pop() : new List<T>(8);
    public static void Return(List<T> list) { list.Clear(); s.Push(list); }
}
