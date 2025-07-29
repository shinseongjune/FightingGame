using System.Collections.Generic;
using UnityEngine;

public class CollisionHandler : MonoBehaviour, ITicker
{
    private CharacterProperty property;

    private readonly List<CollisionEvent> eventQueue = new();

    private readonly List<CollisionEvent> hitCandidates = new();
    private readonly List<CollisionEvent> throwCandidates = new();
    private readonly List<CollisionEvent> guardCandidates = new();

    private Skill alreadyHitSkill;

    private void Start()
    {
        property = GetComponent<CharacterProperty>();
    }

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
            HandleHit(hitCandidates);
            return;
        }

        if (throwCandidates.Count > 0)
        {
            HandleThrow(throwCandidates);
            return;
        }

        if (guardCandidates.Count > 0)
        {
            HandleGuard(ChooseBestGuard(guardCandidates));
            return;
        }
    }

    void HandleHit(List<CollisionEvent> eventList)
    {
        foreach (var e in eventList)
        {
            // �ߺ� ����, ���� üũ, �켱����, ���� �Ǵ� ��
            CharacterProperty attackerProperty = e.attacker.GetComponent<CharacterProperty>();

            if (alreadyHitSkill == attackerProperty.currentSkill) continue;

            Vector2 hitPoint = EstimateHitPoint(e.attackerBox, e.targetBox);
            property.lastHitInfo = new()
            {
                attacker = e.attacker,
                hitBox = e.attackerBox,
                hurtBox = e.targetBox,
                hitPoint = hitPoint,
                direction = e.attackerBox.direction,
                fromFront = (property.isFacingRight && e.attacker.transform.position.x > e.target.transform.position.x)
                            || (!property.isFacingRight && e.attacker.transform.position.x < e.target.transform.position.x),
                region = DetermineRegion(hitPoint),
                damage = attackerProperty.currentSkill.damageOnHit,
                hitStun = attackerProperty.currentSkill.hitstunDuration,
                blockStun = attackerProperty.currentSkill.blockstunDuration,
                launches = attackerProperty.currentSkill.causesLaunch,
                causesKnockdown = attackerProperty.currentSkill.causesKnockdown
            };

            // FSM ���� ���� ��
            return;
        }
    }

    void HandleThrow(List<CollisionEvent> eventList)
    {
        // FSM�� BeingThrownState ���� ��
    }

    void HandleGuard(CollisionEvent e)
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

    bool IsHoldingBack(InputBuffer input, CharacterProperty property)
    {
        if (input.inputQueue.Count == 0)
            return false;

        var latest = input.inputQueue.Peek();
        bool isFacingRight = property.isFacingRight;

        return latest.direction == Direction.Back;
    }

    CollisionEvent ChooseBestGuard(List<CollisionEvent> guards)
    {
        return guards[0];
    }

    Vector2 EstimateHitPoint(BoxComponent hit, BoxComponent hurt)
    {
        // ������ �߾Ӱ� ���
        return (hit.WorldBounds.center + hurt.WorldBounds.center) * 0.5f;
    }

    HitRegion DetermineRegion(Vector2 hitPoint)
    {
        return HitRegion.Body;
        //TODO: �ӽ�ó��. �Ӹ�/����/�ٸ� ��ġ �����ͼ� ���� ��.
    }

    /// <summary>
    /// IdleState OnEnter���� null�� �ʱ�ȭ�� ��.
    /// </summary>
    /// <param name="skill"></param>
    public void SetAlreadyHitSkill(Skill skill)
    {
        alreadyHitSkill = skill;
    }
}
