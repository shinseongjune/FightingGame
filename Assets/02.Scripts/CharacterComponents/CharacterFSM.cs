using System.Collections.Generic;
using UnityEngine;

public class CharacterFSM : MonoBehaviour, ITicker
{
    private BoxPresetApplier boxPresetApplier;

    private CharacterState currentState;
    public CharacterState Current => currentState;

    // 상태 풀링
    private Dictionary<string, CharacterState> statePool = new();

    private void Start()
    {
        boxPresetApplier = GetComponent<BoxPresetApplier>();
    }

    public void RegisterState(string key, CharacterState state)
    {
        if (!statePool.ContainsKey(key))
        {
            statePool[key] = state;
        }
    }

    public void Tick()
    {
        currentState?.OnUpdate();
    }

    public void TransitionTo(string key)
    {
        if (!statePool.TryGetValue(key, out var nextState) || nextState == null || currentState == nextState)
            return;

        boxPresetApplier.ClearAll();

        currentState?.OnExit();
        currentState = nextState;
        currentState?.OnEnter();
    }

    public void ForceSetState(string key)
    {
        if (!statePool.TryGetValue(key, out var nextState) || nextState == null)
            return;

        boxPresetApplier.ClearAll();

        currentState = nextState;
        currentState.OnEnter();
    }
}
