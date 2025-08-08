using System.Collections.Generic;
using UnityEngine;

public class CharacterFSM : MonoBehaviour, ITicker
{
    private CharacterState currentState;
    public CharacterState Current => currentState;

    // 상태 풀링
    private Dictionary<string, CharacterState> statePool = new();

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

        currentState?.OnExit();
        currentState = nextState;
        currentState?.OnEnter();
    }

    public void ForceSetState(string key)
    {
        if (!statePool.TryGetValue(key, out var nextState) || nextState == null)
            return;

        currentState = nextState;
        currentState.OnEnter();
    }
}
