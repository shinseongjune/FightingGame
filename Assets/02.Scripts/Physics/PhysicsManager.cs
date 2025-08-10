using System.Collections.Generic;
using UnityEngine;

public class PhysicsManager : Singleton<PhysicsManager>, ITicker
{
    public float gravity = 9.8f; // or ���ϴ� �߷� ��
    public List<PhysicsEntity> entities = new();

    private int pushIterations = 3;

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
            var g = e.GetComponent<DebugFreeze>();
            g?.OnFreeze(e); // ���� ����ȭ
            if (g != null && g.frozen && g.freezePhysics) { e.transform.position = e.Position; continue; }

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

        ResolveBodyOverlaps();
    }

    private void ResolveBodyOverlaps()
    {
        var list = entities;
        int n = list.Count;
        for (int iter = 0; iter < pushIterations; iter++)
        {
            for (int i = 0; i < n; i++)
            {
                var A = list[i];
                if (A == null || !A.pushboxEnabled || A.currentBodyBox == null) continue;

                for (int j = i + 1; j < n; j++)
                {
                    var B = list[j];
                    if (B == null || !B.pushboxEnabled || B.currentBodyBox == null) continue;
                    if (A == B) continue;

                    // �� �����̳� ��ȣ ���� ���Ͱ� �ִٸ� ���⼭ �ɷ��൵ ����.

                    Rect ra = A.currentBodyBox.GetAABB();
                    Rect rb = B.currentBodyBox.GetAABB();
                    if (!ra.Overlaps(rb)) continue;

                    // ��ħ�� ���
                    float overlapX = Mathf.Min(ra.xMax, rb.xMax) - Mathf.Max(ra.xMin, rb.xMin);
                    float overlapY = Mathf.Min(ra.yMax, rb.yMax) - Mathf.Max(ra.yMin, rb.yMin);

                    // ���� �켱 �и� (�ݰ� Ưȭ)
                    if (overlapX >= overlapY)
                    {
                        // ���� �и�(�� �� ������ ���� Y�� ���� �� �ǵ帲)
                        float push = overlapY * 0.5f;
                        // ����-���� ���̽�: ���߸� ����/�Ʒ��� ��¦ ����
                        if (!A.isGrounded && B.isGrounded) { A.Position += new Vector2(0, push); }
                        else if (A.isGrounded && !B.isGrounded) { B.Position -= new Vector2(0, push); }
                        else
                        {
                            A.Position += new Vector2(0, push);
                            B.Position -= new Vector2(0, push);
                        }
                    }
                    else
                    {
                        // ���� �и�
                        float push = overlapX * 0.5f;
                        // �¿� ���� ����: �߽� ��
                        float dir = Mathf.Sign((A.Position.x) - (B.Position.x));
                        if (dir == 0) dir = (i < j) ? -1f : 1f; // ���� ������ �� ����ȭ��

                        Vector2 dx = new Vector2(push * dir, 0f);
                        A.Position += dx;
                        B.Position -= dx;
                    }

                    // ��ġ ����ȭ
                    A.transform.position = A.Position;
                    B.transform.position = B.Position;
                }
            }
        }
    }
}
