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
        // �з�
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

        // ó�� �켱����
        if (hitCandidates.Count > 0)
        {
            HandleHit(hitCandidates[0]); // ù ��°�� ó��
            return;
        }

        if (throwCandidates.Count > 0)
        {
            HandleThrow(throwCandidates[0]);
            return;
        }

        if (guardCandidates.Count > 0)
        {
            // ���� ���� �ĺ� �� ���� �ĺ� ���� (ex. ���� ����� ����?)
            HandleGuard(ChooseBestGuard(guardCandidates));
            return;
        }
    }

    private void HandleHit(CollisionEvent e)
    {
        // �ߺ� ����, ���� üũ, �켱����, ���� �Ǵ� ��
        var prop = e.target.GetComponent<CharacterProperty>();
        if (prop == null) return;

        // ��: prop.lastHitInfo ����
        // FSM ���� ���� ��
    }

    private void HandleThrow(CollisionEvent e)
    {
        // FSM�� BeingThrownState ���� ��
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
        // �ܼ��� ù ��° ó���ϰų�, �Ʒ�ó�� Ŀ���� ���� ����
        // ��: ���� ����� ���� �� Vector2.Distance(attacker, this)
        return guards[0];
    }
}
