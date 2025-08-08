using System.Collections.Generic;
using UnityEngine;

public class CollisionResolver : MonoBehaviour, ITicker
{
    private readonly List<CollisionData> frameCollisions = new();

    void OnEnable() { BoxManager.Instance.OnCollision += OnCollision; }
    void OnDisable() { BoxManager.Instance.OnCollision -= OnCollision; }

    void OnCollision(CollisionData data)
    {
        frameCollisions.Add(data);
    }

    public void Tick()
    {
        if (frameCollisions.Count == 0) return;

        // 1������ ���� �Ǵ�: ��Ʈ/��� �켱, ����� �ļ��� ����
        // ��Ģ ���� �� ����� CharacterFSM/Property�� �ݿ�
        frameCollisions.Clear();
    }
}
