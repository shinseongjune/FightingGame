using System.Collections.Generic;
using UnityEngine;

public class PhysicsManager : Singleton<PhysicsManager>, ITicker
{
    public float gravity = 9.8f; // or 원하는 중력 값
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
            g?.OnFreeze(e); // 상태 동기화
            if (g != null && g.frozen && g.freezePhysics) { e.transform.position = e.Position; continue; }

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

                    // 팀 구분이나 상호 비대상 필터가 있다면 여기서 걸러줘도 좋음.

                    Rect ra = A.currentBodyBox.GetAABB();
                    Rect rb = B.currentBodyBox.GetAABB();
                    if (!ra.Overlaps(rb)) continue;

                    // 겹침량 계산
                    float overlapX = Mathf.Min(ra.xMax, rb.xMax) - Mathf.Max(ra.xMin, rb.xMin);
                    float overlapY = Mathf.Min(ra.yMax, rb.yMax) - Mathf.Max(ra.yMin, rb.yMin);

                    // 수평 우선 분리 (격겜 특화)
                    if (overlapX >= overlapY)
                    {
                        // 수직 분리(둘 다 지상일 때는 Y를 거의 안 건드림)
                        float push = overlapY * 0.5f;
                        // 공중-지상 케이스: 공중만 위로/아래로 살짝 보정
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
                        // 수평 분리
                        float push = overlapX * 0.5f;
                        // 좌우 방향 결정: 중심 비교
                        float dir = Mathf.Sign((A.Position.x) - (B.Position.x));
                        if (dir == 0) dir = (i < j) ? -1f : 1f; // 완전 동일일 때 안정화용

                        Vector2 dx = new Vector2(push * dir, 0f);
                        A.Position += dx;
                        B.Position -= dx;
                    }

                    // 위치 동기화
                    A.transform.position = A.Position;
                    B.transform.position = B.Position;
                }
            }
        }
    }
}
