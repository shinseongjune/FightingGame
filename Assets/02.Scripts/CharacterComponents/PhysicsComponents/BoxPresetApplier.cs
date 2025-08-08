using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AnimationPlayer), typeof(PhysicsEntity))]
public class BoxPresetApplier : MonoBehaviour, ITicker
{
    public List<BoxComponent> currentBoxes = new();
    private Skill_SO currentSkill;
    private AnimationPlayer animPlayer;
    private PhysicsEntity entity;

    void Awake()
    {
        animPlayer = GetComponent<AnimationPlayer>();
        entity = GetComponent<PhysicsEntity>();
    }

    public void ApplySkill(Skill_SO skill)
    {
        ClearAll();
        currentSkill = skill;
    }

    public void ClearAll()
    {
        for (int i = currentBoxes.Count - 1; i >= 0; --i)
        {
            var box = currentBoxes[i];
            BoxManager.Instance.Unregister(box);
            Destroy(box.gameObject);
        }
        currentBoxes.Clear();
    }

    public void Tick()
    {
        if (currentSkill == null) return;
        int frame = animPlayer.CurrentFrame;

        // 1) 프레임 도달 시 스폰
        foreach (var life in currentSkill.boxLifetimes)
        {
            if (life.startFrame == frame)
            {
                var go = new GameObject($"Box_{currentSkill.name}_{frame}");
                go.transform.SetParent(transform, false);
                var box = go.AddComponent<BoxComponent>();
                box.owner = entity;
                box.type = BoxType.Hit;
                box.offset = life.box.center;
                box.size = life.box.size;
                currentBoxes.Add(box);
                BoxManager.Instance.Register(box);
            }
        }

        // 2) 수명 종료 시 제거
        for (int i = currentBoxes.Count - 1; i >= 0; --i)
        {
            var b = currentBoxes[i];
            var life = currentSkill.boxLifetimes.Find(l => l.box.center == b.offset && l.box.size == b.size);
            if (life.endFrame != 0 && frame > life.endFrame)
            {
                BoxManager.Instance.Unregister(b);
                Destroy(b.gameObject);
                currentBoxes.RemoveAt(i);
            }
        }
    }
}
