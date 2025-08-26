using System.Collections.Generic;
using UnityEngine;

public class BoxPresetApplier : MonoBehaviour
{
    private AnimationPlayer anim;
    private CharacterProperty property;
    private PhysicsEntity entity;

    private Skill_SO activeSkill => property.currentSkill;
    private readonly List<BoxComponent> currentBoxes = new(); // ���� �� �ڽ���

    private void Awake()
    {
        if (!anim) anim = GetComponent<AnimationPlayer>();
        if (!property) property = GetComponent<CharacterProperty>();
        if (!entity) entity = GetComponent<PhysicsEntity>();
    }

    private void OnDisable()
    {
        ClearAllBoxes(); 
    }

    public void ClearAllBoxes()
    {
        for (int i = currentBoxes.Count - 1; i >= 0; i--)
        {
            var b = currentBoxes[i];
            if (b != null)
            {
                if (BoxManager.Instance != null) BoxManager.Instance.Unregister(b);
                Destroy(b.gameObject);
            }
        }
        currentBoxes.Clear();
    }

    public void Tick()
    {
        TrySyncBoxes();
    }

    private void TrySyncBoxes()
    {
        if (activeSkill == null || anim == null || anim.ClipLengthFrames == 0) return;

        // �� Ŭ�� ������ ���� ���
        int cf = anim.CurrentClipFrame;

        // �̹� �����ӿ� "��ȿ�ؾ� �ϴ�" �ڽ� ��� ����
        var wanted = ListStatic<int>.Rent();            // �ε����� ����
        for (int i = 0; i < activeSkill.boxLifetimes.Count; i++)
        {
            var life = activeSkill.boxLifetimes[i];
            // startFrame/endFrame �� "Ŭ�� ������" ����
            if (cf >= life.startFrame && cf <= life.endFrame)
                wanted.Add(i);
        }

        // ���� �ڽ��� ����/����/�߰�
        // �����ϰ�: ���� ����� �ʿ��� �͸� �ٽ� ���� (������ ������ �켱)
        // ���� �ʿ��, key(Ÿ��/����/������)�� diff �Ͽ� �����ϵ��� ����
        if (wanted.Count == 0)
        {
            ClearAllBoxes();
            ListStatic<int>.Return(wanted);
            return;
        }

        // �����
        ClearAllBoxes();

        // �ٽ� ����
        foreach (var idx in wanted)
        {
            var life = activeSkill.boxLifetimes[idx];
            SpawnBox(life, idx);
        }

        ListStatic<int>.Return(wanted);
    }

    private int ComputeStableUid(int lifeIndex)
    {
        unchecked
        {
            int h = entity != null ? entity.GetInstanceID() : 0;
            h = (h * 397) ^ (activeSkill != null ? activeSkill.GetInstanceID() : 0);
            h = (h * 397) ^ lifeIndex;
            return h;
        }
    }

    private void SpawnBox(BoxLifetime life, int lifeIndex)
    {
        if (life.incrementAttackInstance)
            property.attackInstanceId++;

        // �ڽ� ������: Ÿ��/������/������ ���� ������Ʈ �Ծ࿡ ���� �ۼ�
        var go = new GameObject($"Box_{life.type}");
        go.transform.SetParent(transform, false);

        var bc = go.AddComponent<BoxComponent>();
        bc.size = life.box.size;
        bc.type = life.type;
        bc.offset = life.box.center;
        bc.owner = entity;
        bc.sourceSkill = activeSkill;

        bc.uid = ComputeStableUid(lifeIndex);

        currentBoxes.Add(bc);
        BoxManager.Instance.Register(bc);
    }
}

// ������ �ӽ� ����Ʈ Ǯ (GC ���̱�� ���û���)
static class ListStatic<T>
{
    static readonly Stack<List<T>> s = new();
    public static List<T> Rent() => s.Count > 0 ? s.Pop() : new List<T>(8);
    public static void Return(List<T> list) { list.Clear(); s.Push(list); }
}
