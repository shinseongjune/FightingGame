using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public class CharacterFSM : MonoBehaviour, ITicker
{
    private Dictionary<string, CharacterState> statePool = new();

    private CharacterState current;
    public CharacterState CurrentState => current;

    private void Start()
    {
        InitializeStates();

        //TODO: �⺻ ������Ʈ�� fsm�� ����. state������ �޾� ������.
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
}
