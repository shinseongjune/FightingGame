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
        foreach (var e in entities)
        {
            switch (e.mode)
            {
                case PhysicsMode.Normal:
                    if (e.isGravityOn && !e.isGrounded)
                        e.Velocity += Vector2.down * gravity * TickMaster.TICK_INTERVAL;
                    e.Position += e.Velocity * TickMaster.TICK_INTERVAL;
                    // (���� ���� ����)
                    break;

                case PhysicsMode.Kinematic:
                    // �ܺΰ� e.Position�� ���� ����(����/�ִϸ��̼� �̺�Ʈ����)
                    // ���⼭�� �߷�/�̵��� �ǵ帮�� ����.
                    break;

                case PhysicsMode.Carried:
                    if (e.followTarget != null)
                        e.Position = e.followTarget.Position + e.followOffset;
                    // �߷�/�̵�/���� ����
                    break;
            }

            // ... groundY ����(���ϸ� Normal�� ���� ����)
            e.transform.position = e.Position;
        }
    }
}
