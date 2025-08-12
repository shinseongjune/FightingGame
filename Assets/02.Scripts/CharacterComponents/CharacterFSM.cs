using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxPresetApplier))]
public class CharacterFSM : MonoBehaviour
{
    private BoxPresetApplier boxPresetApplier;

    private CharacterState currentState;
    public CharacterState Current => currentState;

    // 상태 풀링: "Idle", "Walk_Forward", "Hit" 등 키로 전이
    private readonly Dictionary<string, CharacterState> statePool = new();

    private void Awake()
    {
        boxPresetApplier = GetComponent<BoxPresetApplier>();
    }

    /// <summary>
    /// 상태 인스턴스를 사전에 등록한다. 보통 생성 시 new XxxState(this)로 만들어 넣는다.
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

        // 상태 틱(프레임 수 증가 포함)
        currentState?.Tick();
    }

    /// <summary>
    /// 문자열 키로 전이. 동일 상태로의 전이는 무시한다.
    /// </summary>
    public void TransitionTo(string key)
    {
        if (!statePool.TryGetValue(key, out var nextState) || nextState == null)
            return;

        if (currentState == nextState) return;

        // 상태별 박스 초기화(히트/허트/트리거 등)
        boxPresetApplier?.ClearAllBoxes();

        // Exit -> 교체 -> Enter
        currentState?.Exit();
        currentState = nextState;
        currentState.Enter();
    }

    /// <summary>
    /// 외부에서 만든 상태 인스턴스로 직접 전이하고 싶을 때.
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
    /// 강제 세팅(Exit 생략 필요할 때). 가급적 TransitionTo 사용 권장.
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
