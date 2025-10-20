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

    private bool _isTransitioning;            // 전이 중 재진입 방지
    private string _pendingKey;               // 대기 중 전이 키(한 번만 처리)
    private bool _hasPending;                 // 대기 여부

    public CharacterState Current => currentState;
    public CharacterState Previous => prevState;

    // 상태 풀링: "Idle", "Walk_Forward", "Hit" 등 키로 전이
    private readonly Dictionary<string, CharacterState> statePool = new();

    private void Awake()
    {
        boxPresetApplier = GetComponent<BoxPresetApplier>();
        property = GetComponent<CharacterProperty>();
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
        // 상태 틱(프레임 수 증가 포함)
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
    /// 문자열 키로 전이. 동일 상태로의 전이는 무시한다.
    /// </summary>
    public void TransitionTo(string key)
    {
        if (!statePool.TryGetValue(key, out var nextState) || nextState == null)
            return;

        if (currentState == nextState) return;

        // 상태별 박스 초기화(히트/허트/트리거 등)
        boxPresetApplier?.ClearAllBoxes();

        prevState = currentState;

        // Exit -> 교체 -> Enter
        currentState?.Exit();
        currentState = nextState;
        currentState.Enter();

        property.NotifyStateEnterForCombo(Current?.StateTag);
    }

    /// <summary>
    /// 외부에서 만든 상태 인스턴스로 직접 전이하고 싶을 때.
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
    /// 강제 세팅(Exit 생략 필요할 때). 가급적 TransitionTo 사용 권장.
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

        // 동일 상태 재요청 무시
        if (Current != null && Current == statePool[key]) return;

        // 마지막 요청 덮어쓰기(최신 상태만 반영)
        _pendingKey = key;
        _hasPending = true;
    }

    private void DoTransition(string key)
    {
        if (Current != null && Current == statePool[key]) return;   // 동일상태 전이 무시

        if (_isTransitioning) return; // 이중진입 방지
        _isTransitioning = true;

        try
        {
            if (!statePool.TryGetValue(key, out var nextState) || nextState == null)
            {
                return;
            }

            prevState = currentState;

            // OnExit/OnEnter 내에서 Transition을 직접 부르지 말 것!
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
