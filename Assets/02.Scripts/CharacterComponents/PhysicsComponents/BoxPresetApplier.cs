using System.Collections.Generic;
using UnityEngine;

public class BoxPresetApplier : MonoBehaviour
{
    private AnimationPlayer anim;
    private CharacterProperty property;
    private PhysicsEntity entity;

    private Skill_SO activeSkill => property.currentSkill;
    private readonly List<BoxComponent> currentBoxes = new(); // 현재 깔린 박스들

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

        // ★ 클립 프레임 기준 사용
        int cf = anim.CurrentClipFrame;

        // 이번 프레임에 "유효해야 하는" 박스 목록 생성
        var wanted = ListStatic<int>.Rent();            // 인덱스만 추적
        for (int i = 0; i < activeSkill.boxLifetimes.Count; i++)
        {
            var life = activeSkill.boxLifetimes[i];
            // startFrame/endFrame 는 "클립 프레임" 기준
            if (cf >= life.startFrame && cf <= life.endFrame)
                wanted.Add(i);
        }

        // 현재 박스를 유지/제거/추가
        // 간단하게: 전부 지우고 필요한 것만 다시 생성 (먼저는 안정성 우선)
        // 성능 필요시, key(타입/센터/사이즈)로 diff 하여 재사용하도록 개선
        if (wanted.Count == 0)
        {
            ClearAllBoxes();
            ListStatic<int>.Return(wanted);
            return;
        }

        // 지우고
        ClearAllBoxes();

        // 다시 생성
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

        // 박스 생성부: 타입/사이즈/오프셋 등은 프로젝트 규약에 맞춰 작성
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

// 간단한 임시 리스트 풀 (GC 줄이기용 선택사항)
static class ListStatic<T>
{
    static readonly Stack<List<T>> s = new();
    public static List<T> Rent() => s.Count > 0 ? s.Pop() : new List<T>(8);
    public static void Return(List<T> list) { list.Clear(); s.Push(list); }
}
