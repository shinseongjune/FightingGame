using System.Collections.Generic;
using UnityEngine;

public enum ResultOption { Rematch, Select, Title, None }

public interface IResultDecisionService
{
    // ���� �� ���� �۽�
    void SendMyChoice(ResultOption option);

    // ��� ���� ���� �̺�Ʈ (UI ������Ʈ��)
    event System.Action<ResultOption> OnPeerChoice;

    // ����/�������� ������ ���� ��� (�̶��� �� ��ȯ)
    event System.Action<ResultOption> OnFinalDecision;

    // (����) ���� �ð� ��� �ܿ� �� ���� �̺�Ʈ
    event System.Action<float> OnServerCountdown;

    // �� ������ �� ���� ����/�絿��ȭ
    void JoinSessionAndSync();

    // �� ���� �� ����
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

        // ���� ���� ���Ǹ� ���� '���'�� ���� ������ ��� �����ϰų�,
        // �׽�Ʈ�� ����/�켱���� ������ ���� �������� ������ ��.
        peer = my;
        OnPeerChoice?.Invoke(peer);

        // ��� �������� ���� UI/�帧 Ȯ��
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

    // ��Ʈ��ũ Ÿ�̸Ӱ� ���� ������ ���� �����
    [SerializeField] private bool useLocalFallbackTimer = true;
    [SerializeField] private float fallbackSeconds = 7f;

    private float localRemain;
    private bool concluded;

    private void Awake()
    {
        localRemain = fallbackSeconds;
        view.SetCountdown(localRemain);

        gm = GameManager.Instance;

        // �Է¸� (Result/Select��)
        gm.actions?.Select.Enable();
        gm.actions?.Player.Disable();
        gm.actions?.UI.Disable();

        // ������ ���� ������, ���߿� ��Ʈ��ũ �������� ��ü
        service = new LocalLoopbackResultDecisionService();

        // ���ε�
        view.OnRematchClicked += _ => service.SendMyChoice(ResultOption.Rematch);
        view.OnSelectClicked += _ => service.SendMyChoice(ResultOption.Select);
        view.OnTitleClicked += _ => service.SendMyChoice(ResultOption.Title);

        service.OnPeerChoice += peer =>
        {
            // ��� ���� ���̶���Ʈ�� �ݿ�
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
