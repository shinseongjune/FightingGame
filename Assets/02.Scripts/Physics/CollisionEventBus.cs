using System.Collections.Generic;
using UnityEngine;

public class CollisionEventBus : Singleton<CollisionEventBus>, ITicker
{
    private readonly List<CollisionEvent> eventQueue = new();

    /// <summary>
    /// PhysicsResolver가 충돌 이벤트를 등록
    /// </summary>
    public void Enqueue(CollisionEvent e)
    {
        eventQueue.Add(e);
    }

    /// <summary>
    /// 매 프레임, 이벤트 큐 전체를 일괄 처리
    /// </summary>
    public void Tick()
    {
        // 1. 캐릭터별 후보군 분류
        var hitMap = new Dictionary<PhysicsEntity, CollisionEvent>();
        var throwMap = new Dictionary<PhysicsEntity, CollisionEvent>();
        var guardMap = new Dictionary<PhysicsEntity, CollisionEvent>();

        foreach (var e in eventQueue)
        {
            switch (e.type)
            {
                case CollisionEventType.Hit:
                    if (!hitMap.ContainsKey(e.target))
                        hitMap[e.target] = e;
                    break;
                case CollisionEventType.Throw:
                    if (!throwMap.ContainsKey(e.target))
                        throwMap[e.target] = e;
                    break;
                case CollisionEventType.Guard:
                    if (!guardMap.ContainsKey(e.target))
                        guardMap[e.target] = e;
                    break;
            }
        }
        eventQueue.Clear();

        // 2. 처리 우선순위: 잡기 > 히트 > 가드
        var alreadyProcessed = new HashSet<PhysicsEntity>();

        // Throw 우선
        foreach (var pair in throwMap)
        {
            var target = pair.Key;
            var e = pair.Value;
            if (alreadyProcessed.Contains(target)) continue;
            ProcessEvent(e);
            alreadyProcessed.Add(target);
        }
        // Hit 다음
        foreach (var pair in hitMap)
        {
            var target = pair.Key;
            var e = pair.Value;
            if (alreadyProcessed.Contains(target)) continue;
            ProcessEvent(e);
            alreadyProcessed.Add(target);
        }
        // Guard 마지막
        foreach (var pair in guardMap)
        {
            var target = pair.Key;
            var e = pair.Value;
            if (alreadyProcessed.Contains(target)) continue;
            ProcessEvent(e);
            alreadyProcessed.Add(target);
        }
    }

    /// <summary>
    /// 실제 피격자 FSM 등으로 이벤트 전달
    /// </summary>
    private void ProcessEvent(CollisionEvent e)
    {
        var property = e.target.GetComponent<CharacterProperty>();
        var fsm = e.target.GetComponent<CharacterFSM>();

        if (property == null || fsm == null) return;

        switch (e.type)
        {
            case CollisionEventType.Hit:
                fsm.OnHit(e);     // FSM의 OnHit 구현 필요
                break;
            case CollisionEventType.Throw:
                fsm.OnThrow(e);   // FSM의 OnThrow 구현 필요
                break;
            case CollisionEventType.Guard:
                fsm.OnGuard(e);   // FSM의 OnGuard 구현 필요
                break;
        }
    }
}
