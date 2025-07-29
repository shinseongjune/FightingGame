using System.Collections.Generic;
using UnityEngine;

public class CollisionEventBus : Singleton<CollisionEventBus>, ITicker
{
    private readonly List<CollisionEvent> eventQueue = new();

    /// <summary>
    /// PhysicsResolver�� �浹 �̺�Ʈ�� ���
    /// </summary>
    public void Enqueue(CollisionEvent e)
    {
        eventQueue.Add(e);
    }

    /// <summary>
    /// �� ������, �̺�Ʈ ť ��ü�� �ϰ� ó��
    /// </summary>
    public void Tick()
    {
        // 1. ĳ���ͺ� �ĺ��� �з�
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

        // 2. ó�� �켱����: ��� > ��Ʈ > ����
        var alreadyProcessed = new HashSet<PhysicsEntity>();

        // Throw �켱
        foreach (var pair in throwMap)
        {
            var target = pair.Key;
            var e = pair.Value;
            if (alreadyProcessed.Contains(target)) continue;
            ProcessEvent(e);
            alreadyProcessed.Add(target);
        }
        // Hit ����
        foreach (var pair in hitMap)
        {
            var target = pair.Key;
            var e = pair.Value;
            if (alreadyProcessed.Contains(target)) continue;
            ProcessEvent(e);
            alreadyProcessed.Add(target);
        }
        // Guard ������
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
    /// ���� �ǰ��� FSM ������ �̺�Ʈ ����
    /// </summary>
    private void ProcessEvent(CollisionEvent e)
    {
        var property = e.target.GetComponent<CharacterProperty>();
        var fsm = e.target.GetComponent<CharacterFSM>();

        if (property == null || fsm == null) return;

        switch (e.type)
        {
            case CollisionEventType.Hit:
                fsm.OnHit(e);     // FSM�� OnHit ���� �ʿ�
                break;
            case CollisionEventType.Throw:
                fsm.OnThrow(e);   // FSM�� OnThrow ���� �ʿ�
                break;
            case CollisionEventType.Guard:
                fsm.OnGuard(e);   // FSM�� OnGuard ���� �ʿ�
                break;
        }
    }
}
