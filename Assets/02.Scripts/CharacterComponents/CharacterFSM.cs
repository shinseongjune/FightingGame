using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public class CharacterFSM : MonoBehaviour, ITicker
{
    [Header("캐릭터 컴포넌트")]
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

        Debug.Log($"[FSM] {current?.GetType().Name} → {next.GetType().Name}");

        current?.OnExit();
        current = next;
        current?.OnEnter();
    }

    public void TransitionTo(string stateName)
    {
        if (!statePool.TryGetValue(stateName, out var next))
        {
            Debug.LogWarning($"[FSM] 상태 '{stateName}' 를 찾을 수 없습니다.");
            return;
        }

        if (current == next) return;

        Debug.Log($"[FSM] {current?.GetType().Name} → {next.GetType().Name}");

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
        // 임시. state들 개선 후 삽입할 것.
    }
}
