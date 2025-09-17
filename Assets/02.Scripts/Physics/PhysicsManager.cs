using NUnit;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsManager : Singleton<PhysicsManager>, ITicker
{
    public float gravity = 9.8f; // or 원하는 중력 값
    public List<PhysicsEntity> entities = new();

    private int pushIterations = 3;

    protected override bool ShouldPersistAcrossScenes() => false;

    public void Register(PhysicsEntity entity)
    {
        if (!entities.Contains(entity))
            entities.Add(entity);
    }

    public void Unregister(PhysicsEntity entity)
    {
        entities.Remove(entity);
    }

    private void OnEnable()
    {
        TickMaster.Instance.Register(this);
    }

    private void OnDisable()
    {
        if (TickMaster.Instance != null) TickMaster.Instance.Unregister(this);
    }

    public void Tick()
    {
        foreach (var e in entities)
        {
            // 만약을 위한 속도 제한
            float maxFall = -20f;
            float maxAirSpeed = 4.0f;
            e.Velocity = new Vector2(
                Mathf.Clamp(e.Velocity.x, -maxAirSpeed, maxAirSpeed),
                Mathf.Max(e.Velocity.y, maxFall)
            );

            switch (e.mode)
            {
                case PhysicsMode.Normal:
                    if (e.isGravityOn && !e.isGrounded)
                        e.Velocity += Vector2.down * gravity * TickMaster.TICK_INTERVAL;
                    e.Position += e.Velocity * TickMaster.TICK_INTERVAL;

                    if (e.Position.y <= 0f)
                    {
                        if (!e.isGrounded)
                        {
                            e.isGrounded = true;
                            e.Position = new Vector2(e.Position.x, 0f);
                            e.Velocity = Vector2.zero;
                        }
                    }
                    else
                    {
                        e.isGrounded = false;
                    }

                    if (e.isGrounded)
                    {
                        // 만약을 위한 마찰 처리
                        e.Velocity = new Vector2(
                            Mathf.MoveTowards(e.Velocity.x, 0f, /*groundFriction*/ 30f * TickMaster.TICK_INTERVAL),
                            0f
                        );
                    }
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

        ResolveBodyOverlaps();
    }

    private void ResolveBodyOverlaps()
    {
        var list = entities;
        for (int iter = 0; iter < pushIterations; iter++)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var A = list[i];
                if (A == null || !A.pushboxEnabled || A.currentBodyBox == null) continue;

                for (int j = i + 1; j < list.Count; j++)
                {
                    var B = list[j];
                    if (B == null || !B.pushboxEnabled || B.currentBodyBox == null) continue;

                    var ra = A.currentBodyBox.GetAABB();
                    var rb = B.currentBodyBox.GetAABB();
                    if (!ra.Overlaps(rb)) continue;

                    float overlapX = Mathf.Min(ra.xMax - rb.xMin, rb.xMax - ra.xMin);
                    float overlapY = Mathf.Min(ra.yMax - rb.yMin, rb.yMax - ra.yMin);

                    bool moveOnlyA = B.immovablePushbox && !A.immovablePushbox;
                    bool moveOnlyB = A.immovablePushbox && !B.immovablePushbox;

                    if (overlapX >= overlapY)
                    {
                        float push = overlapY;
                        if (!A.isGrounded && B.isGrounded) { A.Position += new Vector2(0, push); }
                        else if (A.isGrounded && !B.isGrounded) { B.Position -= new Vector2(0, push); }
                        else
                        {
                            if (moveOnlyA) A.Position += new Vector2(0, push);
                            else if (moveOnlyB) B.Position -= new Vector2(0, push);
                            else { A.Position += new Vector2(0, push * 0.5f); B.Position -= new Vector2(0, push * 0.5f); }
                        }
                    }
                    else
                    {
                        float push = overlapX;
                        float dir = Mathf.Sign(A.Position.x - B.Position.x);
                        if (dir == 0) dir = (i < j) ? -1f : 1f;

                        if (moveOnlyA) A.Position += new Vector2(push * dir, 0);
                        else if (moveOnlyB) B.Position -= new Vector2(push * dir, 0);
                        else { A.Position += new Vector2(push * 0.5f * dir, 0); B.Position -= new Vector2(push * 0.5f * dir, 0); }
                    }

                    A.transform.position = A.Position;
                    B.transform.position = B.Position;
                }
            }
        }
    }
}
