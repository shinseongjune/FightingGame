using System.Collections.Generic;
using UnityEngine;

public class PhysicsManager : Singleton<PhysicsManager>, ITicker
{
    public float gravity = 9.8f; // or ���ϴ� �߷� ��
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
            // �߷�
            if (entity.isGravityOn && !entity.isGrounded)
                entity.Velocity += Vector2.down * gravity * TickMaster.TICK_INTERVAL;

            // �̵�
            entity.Position += entity.Velocity * TickMaster.TICK_INTERVAL;

            // ���� ����
            if (entity.Position.y <= entity.groundY) // �ٴ� ����
            {
                if (!entity.isGrounded)
                {
                    // ���� �̺�Ʈ �߻�(FSM ���� ���� ��)
                    entity.isGrounded = true;
                    entity.Position = new Vector2(entity.Position.x, entity.groundY);
                    entity.Velocity = new Vector2(entity.Velocity.x, 0f);

                    // ���¸ӽſ� �뺸�ϰų�, OnLand �̺�Ʈ ȣ�� ����
                }
                else
                {
                    // �̹� ���� ���¶�� y/fall�ӵ� ������
                    entity.Position = new Vector2(entity.Position.x, entity.groundY);
                    entity.Velocity = new Vector2(entity.Velocity.x, 0f);
                }
            }
            else
            {
                entity.isGrounded = false;
            }

            // ��ġ ����ȭ
            entity.transform.position = entity.Position;
        }
    }
}
