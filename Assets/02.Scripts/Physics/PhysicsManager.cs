using System.Collections.Generic;
using UnityEngine;

public class PhysicsManager : Singleton<PhysicsManager>, ITicker
{
    public float gravity = 9.8f; // or 원하는 중력 값
    public List<PhysicsEntity> entities = new();

    public void Register(PhysicsEntity entity)
    {
        if (!entities.Contains(entity))
            entities.Add(entity);
    }

    public void Unregister(PhysicsEntity entity)
    {
        entities.Remove(entity);
    }

    public void Tick()
    {
        foreach (var e in entities)
        {
            switch (e.mode)
            {
                case PhysicsMode.Normal:
                    if (e.isGravityOn && !e.isGrounded)
                        e.Velocity += Vector2.down * gravity * TickMaster.TICK_INTERVAL;
                    e.Position += e.Velocity * TickMaster.TICK_INTERVAL;
                    // (착지 판정 유지)
                    break;

                case PhysicsMode.Kinematic:
                    // 외부가 e.Position을 직접 제어(상태/애니메이션 이벤트에서)
                    // 여기서는 중력/이동을 건드리지 않음.
                    break;

                case PhysicsMode.Carried:
                    if (e.followTarget != null)
                        e.Position = e.followTarget.Position + e.followOffset;
                    // 중력/이동/착지 무시
                    break;
            }

            // ... groundY 판정(원하면 Normal일 때만 수행)
            e.transform.position = e.Position;
        }
    }
}
