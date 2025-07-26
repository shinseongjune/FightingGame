using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 현재 스킬과 애니메이션 프레임에 따라 박스를 생성/적용하는 컴포넌트
/// </summary>
[RequireComponent(typeof(AnimationPlayer))]
public class BoxPresetApplier : MonoBehaviour, ITicker
{
    public Skill currentSkill;

    private AnimationPlayer animPlayer;
    private readonly List<BoxComponent> currentBoxes = new();

    private void Awake()
    {
        animPlayer = GetComponent<AnimationPlayer>();
    }

    public void Tick()
    {
        ClearExpiredBoxes();

        if (currentSkill == null || currentSkill.boxLifetimes == null)
            return;

        int frame = animPlayer.CurrentFrame;

        foreach (var entry in currentSkill.boxLifetimes)
        {
            if (entry.startFrame == frame)
            {
                var go = new GameObject($"Box_{entry.box.type}");
                go.transform.SetParent(transform);
                go.transform.localPosition = entry.box.center;

                var box = go.AddComponent<BoxComponent>();
                box.type = entry.box.type;
                box.center = Vector2.zero;
                box.size = entry.box.size;
                box.layer = entry.box.layer;

                // 수명 정보 저장용 태그
                go.AddComponent<FrameTag>().endFrame = entry.endFrame;

                currentBoxes.Add(box);
            }
        }
    }

    private void ClearExpiredBoxes()
    {
        List<BoxComponent> deleteList = new List<BoxComponent>();

        int frame = animPlayer.CurrentFrame;
        for (int i = currentBoxes.Count - 1; i >= 0; i--)
        {
            var box = currentBoxes[i];
            var tag = box.GetComponent<FrameTag>();
            if (tag != null && frame >= tag.endFrame)
            {
                deleteList.Add(box);
            }
        }

        foreach (var box in deleteList)
        {
            currentBoxes.Remove(box);
            Destroy(box.gameObject);
        }
    }

    private class FrameTag : MonoBehaviour
    {
        public int endFrame;
    }

    public void ClearAllBoxes()
    {
        for (int i = currentBoxes.Count - 1; i >= 0; i--)
        {
            var box = currentBoxes[i];
            if (box != null)
                Destroy(box.gameObject);
            currentBoxes.RemoveAt(i);
        }
    }
}
