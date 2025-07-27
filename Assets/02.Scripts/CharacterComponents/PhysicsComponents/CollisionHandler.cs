using System.Collections.Generic;
using UnityEngine;

public class CollisionHandler : MonoBehaviour, ITicker
{
    private readonly List<CollisionEvent> eventQueue = new();

    private readonly List<CollisionEvent> hitCandidates = new();
    private readonly List<CollisionEvent> throwCandidates = new();
    private readonly List<CollisionEvent> guardCandidates = new();

    private bool wasHitThisFrame = false;

    public void Enqueue(CollisionEvent e)
    {
        eventQueue.Add(e);
    }

    public void Tick()
    {
        // 분류
        hitCandidates.Clear();
        throwCandidates.Clear();
        guardCandidates.Clear();

        foreach (var e in eventQueue)
        {
            switch (e.type)
            {
                case CollisionEventType.Hit:
                    hitCandidates.Add(e);
                    break;
                case CollisionEventType.Throw:
                    throwCandidates.Add(e);
                    break;
                case CollisionEventType.Guard:
                    guardCandidates.Add(e);
                    break;
            }
        }

        eventQueue.Clear();

        // 처리 우선순위
        if (hitCandidates.Count > 0)
        {
            HandleHit(hitCandidates[0]); // 첫 번째만 처리
            return;
        }

        if (throwCandidates.Count > 0)
        {
            HandleThrow(throwCandidates[0]);
            return;
        }

        if (guardCandidates.Count > 0)
        {
            // 여러 가드 후보 중 최적 후보 선택 (ex. 가장 가까운 위협?)
            HandleGuard(ChooseBestGuard(guardCandidates));
            return;
        }
    }

    private void HandleHit(CollisionEvent e)
    {
        // 중복 필터, 무적 체크, 우선순위, 방향 판단 등
        var prop = e.target.GetComponent<CharacterProperty>();
        if (prop == null) return;

        // 예: prop.lastHitInfo 갱신
        // FSM 상태 전이 등
    }

    private void HandleThrow(CollisionEvent e)
    {
        // FSM에 BeingThrownState 전이 등
    }

    private void HandleGuard(CollisionEvent e)
    {
        var fsm = e.target.GetComponent<CharacterFSM>();
        var input = e.target.GetComponent<InputBuffer>();
        var property = e.target.GetComponent<CharacterProperty>();

        if (fsm == null || input == null || property == null)
            return;

        bool holdingBack = IsHoldingBack(input, property);
        if (holdingBack)
        {
            fsm.TransitionTo(new GuardState(fsm));
        }
    }

    private bool IsHoldingBack(InputBuffer input, CharacterProperty property)
    {
        if (input.inputQueue.Count == 0)
            return false;

        var latest = input.inputQueue.Peek();
        bool isFacingRight = property.isFacingRight;

        return (isFacingRight && latest.direction == Direction.Back) ||
               (!isFacingRight && latest.direction == Direction.Forward);
    }

    private CollisionEvent ChooseBestGuard(List<CollisionEvent> guards)
    {
        // 단순히 첫 번째 처리하거나, 아래처럼 커스텀 로직 가능
        // 예: 가장 가까운 위협 → Vector2.Distance(attacker, this)
        return guards[0];
    }
}
