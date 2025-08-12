using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxPresetApplier))]
public class CharacterFSM : MonoBehaviour
{
    private BoxPresetApplier boxPresetApplier;

    private CharacterState currentState;
    public CharacterState Current => currentState;

    // ���� Ǯ��: "Idle", "Walk_Forward", "Hit" �� Ű�� ����
    private readonly Dictionary<string, CharacterState> statePool = new();

    private void Awake()
    {
        boxPresetApplier = GetComponent<BoxPresetApplier>();
    }

    /// <summary>
    /// ���� �ν��Ͻ��� ������ ����Ѵ�. ���� ���� �� new XxxState(this)�� ����� �ִ´�.
    /// </summary>
    public void RegisterState(string key, CharacterState state)
    {
        if (string.IsNullOrEmpty(key) || state == null) return;
        if (!statePool.ContainsKey(key))
            statePool[key] = state;
    }

    public void Tick()
    {
        var gate = GetComponent<DebugFreeze>();
        if (gate != null && gate.IsFrozen()) return;

        // ���� ƽ(������ �� ���� ����)
        currentState?.Tick();
    }

    /// <summary>
    /// ���ڿ� Ű�� ����. ���� ���·��� ���̴� �����Ѵ�.
    /// </summary>
    public void TransitionTo(string key)
    {
        if (!statePool.TryGetValue(key, out var nextState) || nextState == null)
            return;

        if (currentState == nextState) return;

        // ���º� �ڽ� �ʱ�ȭ(��Ʈ/��Ʈ/Ʈ���� ��)
        boxPresetApplier?.ClearAllBoxes();

        // Exit -> ��ü -> Enter
        currentState?.Exit();
        currentState = nextState;
        currentState.Enter();
    }

    /// <summary>
    /// �ܺο��� ���� ���� �ν��Ͻ��� ���� �����ϰ� ���� ��.
    /// </summary>
    public void TransitionTo(CharacterState nextState)
    {
        if (nextState == null || currentState == nextState) return;

        boxPresetApplier?.ClearAllBoxes();

        currentState?.Exit();
        currentState = nextState;
        currentState.Enter();
    }

    /// <summary>
    /// ���� ����(Exit ���� �ʿ��� ��). ������ TransitionTo ��� ����.
    /// </summary>
    public void ForceSetState(string key)
    {
        if (!statePool.TryGetValue(key, out var nextState) || nextState == null)
            return;

        boxPresetApplier?.ClearAllBoxes();

        currentState = nextState;
        currentState.Enter();
    }

    public T GetState<T>(string key) where T : CharacterState
    {
        return statePool.TryGetValue(key, out var s) ? s as T : null;
    }
}
