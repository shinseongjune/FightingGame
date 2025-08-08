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

        // 1프레임 종합 판단: 히트/잡기 우선, 가드는 후순위 집계
        // 규칙 적용 → 결과를 CharacterFSM/Property에 반영
        frameCollisions.Clear();
    }
}
