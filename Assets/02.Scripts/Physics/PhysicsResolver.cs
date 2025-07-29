using System.Collections.Generic;
using UnityEngine;

public class PhysicsResolver : Singleton<PhysicsResolver>, ITicker
{
    private readonly List<PhysicsEntity> entities = new();

    public void Register(PhysicsEntity entity)
    {
        if (entity != null && !entities.Contains(entity))
            entities.Add(entity);
    }

    public void Unregister(PhysicsEntity entity)
    {
        if (entity != null)
            entities.Remove(entity);
    }

    public void Tick()
    {
        var totalOffset = new Dictionary<PhysicsEntity, Vector2>();

        // 1. BodyBox 충돌 감지 및 해소
        for (int i = 0; i < entities.Count; i++)
        {
            var a = entities[i];
            var aBox = a.BodyBox;
            if (aBox == null || !aBox.IsEnabled) continue;

            for (int j = i + 1; j < entities.Count; j++)
            {
                var b = entities[j];
                var bBox = b.BodyBox;
                if (bBox == null || !bBox.IsEnabled) continue;

                if (a.transform.root == b.transform.root) continue;
                if (!aBox.WorldBounds.Overlaps(bBox.WorldBounds)) continue;

                Vector2 resolution = ResolveX(aBox.WorldBounds, bBox.WorldBounds);
                if (resolution == Vector2.zero) continue;

                bool aMovable = a.velocity != Vector2.zero;
                bool bMovable = b.velocity != Vector2.zero;

                if (!aMovable && !bMovable) continue;

                if (!totalOffset.ContainsKey(a)) totalOffset[a] = Vector2.zero;
                if (!totalOffset.ContainsKey(b)) totalOffset[b] = Vector2.zero;

                if (aMovable && bMovable)
                {
                    totalOffset[a] += resolution * 0.5f;
                    totalOffset[b] -= resolution * 0.5f;
                }
                else if (aMovable)
                {
                    totalOffset[a] += resolution;
                }
                else if (bMovable)
                {
                    totalOffset[b] -= resolution;
                }
            }
        }

        // 2. 히트 / 잡기 / 가드 유발 박스 감지
        foreach (var attacker in entities)
        {
            foreach (var attackBox in attacker.Boxes)
            {
                if (!attackBox.IsEnabled || !attackBox.IsTrigger) continue;

                foreach (var target in entities)
                {
                    if (attacker == target) continue;

                    foreach (var targetBox in target.Boxes)
                    {
                        if (!targetBox.IsEnabled || targetBox == attackBox) continue;
                        if (!attackBox.WorldBounds.Overlaps(targetBox.WorldBounds)) continue;

                        var evtType = GetCollisionType(attackBox.type, targetBox.type);
                        if (evtType == null) continue;

                        if (target.TryGetComponent(out CharacterProperty property))
                        {
                            // 전역 버스에 전달
                            CollisionEventBus.Instance.Enqueue(new CollisionEvent
                            {
                                type = evtType.Value,
                                attacker = attacker,
                                attackerBox = attackBox,
                                target = target,
                                targetBox = targetBox,
                            });
                        }
                    }
                }
            }
        }

        // 3. 위치 해소 적용
        foreach (var kv in totalOffset)
        {
            kv.Key.ApplyOffset(kv.Value);
        }

        // 4. 이동 속도 적용
        foreach (var entity in entities)
        {
            entity.ApplyVelocity();
        }
    }

    /// <summary>
    /// x축으로 밀어내는 해소 벡터 계산
    /// </summary>
    private Vector2 ResolveX(Rect a, Rect b)
    {
        float xOverlap = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);

        if (xOverlap <= 0)
            return Vector2.zero;

        float pushX = (a.center.x < b.center.x) ? -xOverlap : xOverlap;
        return new Vector2(pushX, 0);
    }

    private CollisionEventType? GetCollisionType(BoxType attackerType, BoxType targetType)
    {
        return (attackerType, targetType) switch
        {
            (BoxType.Hit, BoxType.Hurt) => CollisionEventType.Hit,
            (BoxType.Throw, BoxType.Body) => CollisionEventType.Throw,
            (BoxType.GuardTrigger, BoxType.Body) => CollisionEventType.Guard,
            _ => null
        };
    }
}
