using System.Collections.Generic;
using UnityEngine;

public enum ResultOption { Rematch, Select, Title, None }

public interface IResultDecisionService
{
    // 내가 고른 선택 송신
    void SendMyChoice(ResultOption option);

    // 상대 선택 도착 이벤트 (UI 업데이트용)
    event System.Action<ResultOption> OnPeerChoice;

    // 서버/권위에서 내려온 최종 결과 (이때만 씬 전환)
    event System.Action<ResultOption> OnFinalDecision;

    // (선택) 서버 시간 기반 잔여 초 갱신 이벤트
    event System.Action<float> OnServerCountdown;

    // 씬 들어왔을 때 세션 조인/재동기화
    void JoinSessionAndSync();

    // 씬 떠날 때 정리
    void Dispose();
}

public sealed class LocalLoopbackResultDecisionService : IResultDecisionService
{
    public event System.Action<ResultOption> OnPeerChoice;
    public event System.Action<ResultOption> OnFinalDecision;
    public event System.Action<float> OnServerCountdown;

    private float countdown = 7f;
    private ResultOption my = ResultOption.None;
    private ResultOption peer = ResultOption.None;
    private bool concluded;

    public void JoinSessionAndSync() { /* no-op */ }

    public void SendMyChoice(ResultOption option)
    {
        if (concluded) return;
        my = option;

        // 로컬 개발 편의를 위해 '상대'도 같은 값으로 즉시 에코하거나,
        // 테스트용 랜덤/우선순위 로직을 돌려 최종값을 내려도 됨.
        peer = my;
        OnPeerChoice?.Invoke(peer);

        // 즉시 최종결정 내려 UI/흐름 확인
        concluded = true;
        OnFinalDecision?.Invoke(DecideWithPriority(my, peer));
    }

    public void Dispose() { }

    private ResultOption DecideWithPriority(ResultOption a, ResultOption b)
    {
        if (a == ResultOption.None && b == ResultOption.None) return ResultOption.Rematch;
        if (a == ResultOption.Title || b == ResultOption.Title) return ResultOption.Title;
        if (a == ResultOption.Select || b == ResultOption.Select) return ResultOption.Select;
        return ResultOption.Rematch;
    }
}

public sealed class ResultSceneController : MonoBehaviour
{
    [SerializeField] private ResultSceneView view;
    private GameManager gm;
    private IResultDecisionService service;

    // 네트워크 타이머가 오기 전까지 로컬 폴백용
    [SerializeField] private bool useLocalFallbackTimer = true;
    [SerializeField] private float fallbackSeconds = 7f;

    private float localRemain;
    private bool concluded;

    private void Awake()
    {
        localRemain = fallbackSeconds;
        view.SetCountdown(localRemain);

        gm = GameManager.Instance;

        // 입력맵 (Result/Select만)
        gm.actions?.Select.Enable();
        gm.actions?.Player.Disable();
        gm.actions?.UI.Disable();

        // 지금은 로컬 루프백, 나중에 네트워크 구현으로 교체
        service = new LocalLoopbackResultDecisionService();

        // 바인딩
        view.OnRematchClicked += _ => service.SendMyChoice(ResultOption.Rematch);
        view.OnSelectClicked += _ => service.SendMyChoice(ResultOption.Select);
        view.OnTitleClicked += _ => service.SendMyChoice(ResultOption.Title);

        service.OnPeerChoice += peer =>
        {
            // 상대 선택 하이라이트만 반영
            view.ShowOppRematch(peer == ResultOption.Rematch);
            view.ShowOppSelect(peer == ResultOption.Select);
            view.ShowOppTitle(peer == ResultOption.Title);
        };

        service.OnFinalDecision += ResolveAndGo;
        service.OnServerCountdown += sec => view.SetCountdown(sec);

        service.JoinSessionAndSync();
    }

    private void Update()
    {
        if (concluded) return;

        if (useLocalFallbackTimer)
        {
            localRemain -= Time.unscaledDeltaTime;
            if (localRemain < 0f) localRemain = 0f;
            view.SetCountdown(localRemain);
        }
    }

    private void OnDestroy()
    {
        view.OnRematchClicked -= _ => service.SendMyChoice(ResultOption.Rematch);
        view.OnSelectClicked -= _ => service.SendMyChoice(ResultOption.Select);
        view.OnTitleClicked -= _ => service.SendMyChoice(ResultOption.Title);

        gm.actions?.Select.Disable();
        service?.Dispose();
    }

    private void ResolveAndGo(ResultOption final)
    {
        concluded = true;

        gm.lastResult = null;

        switch (final)
        {
            case ResultOption.Rematch: SceneLoader.Instance.LoadScene("BattleScene"); break;
            case ResultOption.Select: SceneLoader.Instance.LoadScene("SelectScene"); break;
            case ResultOption.Title: SceneLoader.Instance.LoadScene("TitleScene"); break;
            default: SceneLoader.Instance.LoadScene("TitleScene"); break;
        }
    }
}
