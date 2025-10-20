using System.Collections.Generic;
using UnityEngine;
using static Codice.CM.Common.CmCallContext;

[RequireComponent(typeof(BoxPresetApplier))]
public class CharacterFSM : MonoBehaviour
{
    private BoxPresetApplier boxPresetApplier;
    private CharacterProperty property;

    private CharacterState currentState;
    private CharacterState prevState;

    private bool _isTransitioning;            // ���� �� ������ ����
    private string _pendingKey;               // ��� �� ���� Ű(�� ���� ó��)
    private bool _hasPending;                 // ��� ����

    public CharacterState Current => currentState;
    public CharacterState Previous => prevState;

    // ���� Ǯ��: "Idle", "Walk_Forward", "Hit" �� Ű�� ����
    private readonly Dictionary<string, CharacterState> statePool = new();

    private void Awake()
    {
        boxPresetApplier = GetComponent<BoxPresetApplier>();
        property = GetComponent<CharacterProperty>();
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
        // ���� ƽ(������ �� ���� ����)
        currentState?.Tick();

        if (_hasPending && !_isTransitioning)
        {
            var key = _pendingKey;
            _pendingKey = null;
            _hasPending = false;
            DoTransition(key);
        }
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

        prevState = currentState;

        // Exit -> ��ü -> Enter
        currentState?.Exit();
        currentState = nextState;
        currentState.Enter();

        property.NotifyStateEnterForCombo(Current?.StateTag);
    }

    /// <summary>
    /// �ܺο��� ���� ���� �ν��Ͻ��� ���� �����ϰ� ���� ��.
    /// </summary>
    public void TransitionTo(CharacterState nextState)
    {
        if (nextState == null || currentState == nextState) return;

        boxPresetApplier?.ClearAllBoxes();

        prevState = currentState;

        currentState?.Exit();
        currentState = nextState;
        currentState.Enter();

        property.NotifyStateEnterForCombo(Current?.StateTag);
    }

    /// <summary>
    /// ���� ����(Exit ���� �ʿ��� ��). ������ TransitionTo ��� ����.
    /// </summary>
    public void ForceSetState(string key)
    {
        if (!statePool.TryGetValue(key, out var nextState) || nextState == null)
            return;

        boxPresetApplier?.ClearAllBoxes();

        prevState = currentState;

        currentState = nextState;
        currentState.Enter();

        property?.NotifyStateEnterForCombo(Current?.StateTag);
    }

    public T GetState<T>(string key) where T : CharacterState
    {
        return statePool.TryGetValue(key, out var s) ? s as T : null;
    }

    public void RequestTransition(string key)
    {
        if (string.IsNullOrEmpty(key)) return;

        // ���� ���� ���û ����
        if (Current != null && Current == statePool[key]) return;

        // ������ ��û �����(�ֽ� ���¸� �ݿ�)
        _pendingKey = key;
        _hasPending = true;
    }

    private void DoTransition(string key)
    {
        if (Current != null && Current == statePool[key]) return;   // ���ϻ��� ���� ����

        if (_isTransitioning) return; // �������� ����
        _isTransitioning = true;

        try
        {
            if (!statePool.TryGetValue(key, out var nextState) || nextState == null)
            {
                return;
            }

            prevState = currentState;

            // OnExit/OnEnter ������ Transition�� ���� �θ��� �� ��!
            currentState?.Exit();
            currentState = nextState;
            currentState?.Enter();

            property.NotifyStateEnterForCombo(Current?.StateTag);
        }
        finally
        {
            _isTransitioning = false;
        }
    }
}
