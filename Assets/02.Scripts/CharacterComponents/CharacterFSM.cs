using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterFSM : MonoBehaviour, ITicker
{
    private Dictionary<string, CharacterState> statePool = new();

    private CharacterState current;
    public CharacterState CurrentState => current;

    private void Start()
    {
        InitializeStates();

        //TODO: 기본 컴포넌트들 fsm에 내장. state에서는 받아 쓰도록.
    }

    public void Tick()
    {
        current?.OnUpdate();
    }

    [Obsolete]
    public void TransitionTo(CharacterState next)
    {
        if (next == null) return;

        Debug.Log($"[FSM] {current?.GetType().Name} → {next.GetType().Name}");

        current?.OnExit();
        current = next;
        current?.OnEnter();
    }

    public void TransitionTo(string next)
    {
        if (!statePool.ContainsKey(next))
        {
            return;
        }

        Debug.Log($"[FSM] {current?.GetType().Name} → {next.GetType().Name}");

        current?.OnExit();
        current = statePool[next];
        current?.OnEnter();
    }

    public void InitializeStates()
    {
        //statePool["Idle"] = new IdleState(this);
        //statePool["Walk"] = new WalkState(this);
        //statePool["Skill"] = new SkillState(this);
        //statePool["HitStun"] = new HitStunState(this);
        // 임시. state들 개선 후 삽입할 것.
    }
}
