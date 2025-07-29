using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public class CharacterFSM : MonoBehaviour, ITicker
{
    [Header("ĳ���� ������Ʈ")]
    private CharacterProperty property;
    private SkillExecutor skillExecutor;
    private BoxPresetApplier boxPresetApplier;
    private AnimationPlayer animPlayer;
    private PhysicsEntity physicsEntity;

    private Dictionary<string, CharacterState> statePool = new();

    private CharacterState current;
    public CharacterState CurrentState => current;

    private void Start()
    {
        InitializeStates();

        property = GetComponent<CharacterProperty>();
        skillExecutor = GetComponent<SkillExecutor>();
        boxPresetApplier = GetComponent<BoxPresetApplier>();
        animPlayer = GetComponent<AnimationPlayer>();
        physicsEntity = GetComponent<PhysicsEntity>();
    }

    public void Tick()
    {
        current?.OnUpdate();
    }

    [Obsolete]
    public void TransitionTo(CharacterState next)
    {
        if (next == null) return;

        Debug.Log($"[FSM] {current?.GetType().Name} �� {next.GetType().Name}");

        current?.OnExit();
        current = next;
        current?.OnEnter();
    }

    public void TransitionTo(string stateName)
    {
        if (!statePool.TryGetValue(stateName, out var next))
        {
            Debug.LogWarning($"[FSM] ���� '{stateName}' �� ã�� �� �����ϴ�.");
            return;
        }

        if (current == next) return;

        Debug.Log($"[FSM] {current?.GetType().Name} �� {next.GetType().Name}");

        current?.OnExit();
        current = next;
        current?.OnEnter();
    }

    public void InitializeStates()
    {
        //statePool["Idle"] = new IdleState(this);
        //statePool["Walk"] = new WalkState(this);
        //statePool["Skill"] = new SkillState(this);
        //statePool["HitStun"] = new HitStunState(this);
        // �ӽ�. state�� ���� �� ������ ��.
    }

    public void OnHit(CollisionEvent e)
    {
        var property = GetComponent<CharacterProperty>();

        // 1. �̹� ���� hitId�� �ǰݵƴ��� Ȯ�� (�ߺ� ����)
        if (property.HasHitId(e.attackerBox.hitId))
            return;

        // 2. ����, ���۾Ƹ� �� ���� üũ
        if (property.isInvincible ||
            property.invincibleType == InvincibleType.All ||
            (property.invincibleType == InvincibleType.AirAttack && e.attackerBox.isAirAttack))
        {
            Debug.Log("���� ���·� �ǰ� ����!");
            return;
        }

        // 3. ���۾Ƹ�
        if (property.superArmorCount > 0)
        {
            property.superArmorCount--;
            // ����� ����, ��Ʈ���� ����
            // ... (����� ���/����)
            // (��Ʈ��ž/����/��ƼŬ �� ���� ����)
            property.AddHitId(e.attackerBox.hitId);
            Debug.Log("���۾Ƹӷ� ����! ���� ���۾Ƹ�: " + property.superArmorCount);
            return;
        }

        // 4. ���� �ǰ� ����
        // (�����ϴ� ����: ���߰����̸� ��ġ����, �ƴϸ� ������)
        HitRegion hitRegion = GetActualHitRegion(e.attackerBox, property, e);

        // ���� ���� ���ε� ���⼭ �б� ����!
        // if (CanGuard(hitRegion, property)) { ... }

        // �����, ���� ����, ��Ʈ��ž �� ���� ����
        property.AddHitId(e.attackerBox.hitId);
        property.comboCount++;
        // ���� ���� ��
        Debug.Log("�ǰ�! ���� ����/����/����� ����");
        // ��: ���� ���� �� this.TransitionTo("HitStun");
    }
    public void OnThrow(CollisionEvent e)
    {
        var property = GetComponent<CharacterProperty>();
        // ���� ó��: ��� ���� ���� ����, ����� ��
        Debug.Log("����! ���� ����");
        // ��: this.TransitionTo("BeingThrown");
    }
    public void OnGuard(CollisionEvent e)
    {
        var property = GetComponent<CharacterProperty>();
        // ���� ���� ����: ��Ͻ���, ����, ���� ����
        Debug.Log("���� ����!");
        // ���� ���� ��
    }

    // �����ϴ� ���� �Լ�
    private HitRegion GetActualHitRegion(BoxComponent attackerBox, CharacterProperty target, CollisionEvent e)
    {
        if (attackerBox.isAirAttack)
            return DetermineRegion(e.attackerBox.WorldBounds.center, target);
        else
            return attackerBox.hitRegion;
    }

    private HitRegion DetermineRegion(Vector2 hitPoint, CharacterProperty target)
    {
        float y = hitPoint.y;
        float headY = target.headPoint.position.y;
        float bodyY = target.bodyPoint.position.y;
        float legsY = target.legsPoint.position.y;
        if (y > (headY + bodyY) / 2f) return HitRegion.Head;
        if (y < (bodyY + legsY) / 2f) return HitRegion.Legs;
        return HitRegion.Body;
    }
}
