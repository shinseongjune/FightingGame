using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ���� ��ų�� �ִϸ��̼� �����ӿ� ���� �ڽ��� ����/�����ϴ� ������Ʈ
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
        if (currentSkill == null || currentSkill.frameToBoxes == null)
        {
            ClearBoxes();
            return;
        }

        int currentFrame = animPlayer.CurrentFrame;

        if (!currentSkill.frameToBoxes.TryGetBoxes(currentFrame, out var boxDataList))
        {
            ClearBoxes();
            return;
        }

        ApplyBoxes(boxDataList);
    }

    private void ClearBoxes()
    {
        foreach (var box in currentBoxes)
        {
            if (box != null)
                Destroy(box.gameObject);
        }
        currentBoxes.Clear();
    }

    private void ApplyBoxes(BoxData[] boxDataList)
    {
        ClearBoxes();

        foreach (var data in boxDataList)
        {
            GameObject go = new GameObject($"Box_{data.type}");
            go.transform.SetParent(transform);
            go.transform.localPosition = data.center;

            BoxComponent box = go.AddComponent<BoxComponent>();
            box.type = data.type;
            box.center = Vector2.zero; // �̹� localPosition���� �ݿ�
            box.size = data.size;
            box.layer = data.layer;

            currentBoxes.Add(box);
        }
    }
}
