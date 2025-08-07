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
        foreach (var entity in entities)
        {
            // 중력
            if (entity.isGravityOn && !entity.isGrounded)
                entity.Velocity += Vector2.down * gravity * TickMaster.TICK_INTERVAL;

            // 이동
            entity.Position += entity.Velocity * TickMaster.TICK_INTERVAL;

            // 지상 판정
            if (entity.Position.y <= entity.groundY) // 바닥 닿음
            {
                if (!entity.isGrounded)
                {
                    // 착지 이벤트 발생(FSM 상태 전이 등)
                    entity.isGrounded = true;
                    entity.Position = new Vector2(entity.Position.x, entity.groundY);
                    entity.Velocity = new Vector2(entity.Velocity.x, 0f);

                    // 상태머신에 통보하거나, OnLand 이벤트 호출 가능
                }
                else
                {
                    // 이미 착지 상태라면 y/fall속도 보정만
                    entity.Position = new Vector2(entity.Position.x, entity.groundY);
                    entity.Velocity = new Vector2(entity.Velocity.x, 0f);
                }
            }
            else
            {
                entity.isGrounded = false;
            }

            // 위치 동기화
            entity.transform.position = entity.Position;
        }
    }
}
