using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// - ������ ����, ����(��/�ߺ�), Ÿ�̸�, ��� Ŀ��, �� ���ε�/�ǵ��
/// - �׺���̼�(���� ��Ŀ�� ����)�� GeometryNavigator���� ����
/// </summary>
public sealed class SelectSceneController : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] int columns = 6;

    [Header("Timer (optional)")]
    [SerializeField] bool useCharacterTimeout = false;
    [SerializeField] float characterTimeoutSec = 0f;
    [SerializeField] bool useStageTimeout = false;
    [SerializeField] float stageTimeoutSec = 0f;

    ISelectSceneView _view;
    ISelectSceneModel _model;
    GeometryNavigator _navigator = new();

    enum Phase { NavCharacters, ConfirmedCharacters, NavStages, ConfirmedStages, TransitionOut }
    Phase _phase = Phase.NavCharacters;

    // ��Ŀ��/���� ����
    int _focusP1, _focusP2;               // ���� ��Ŀ�� id (NodeGeom.id)
    string _p1CharId, _p2CharId, _stageId;

    float _timer;                          // ���� �������� ���� �ð�(�ɼ�)

    // ====== �ܺ� �ʱ�ȭ ======
    public void Initialize(ISelectSceneView view, ISelectSceneModel model)
    {
        _view = view;
        _model = model;

        // �̺�Ʈ �輱
        _view.OnViewReady += OnViewReady;
        _view.OnNavigate += OnNavigate;
        _view.OnSubmit += OnSubmit;
        _view.OnCancel += OnCancel;
        _view.OnRandom += OnRandom;
        _view.OnHoverIndexChanged += OnHover;
    }

    void OnDestroy()
    {
        if (_view == null) return;
        _view.OnViewReady -= OnViewReady;
        _view.OnNavigate -= OnNavigate;
        _view.OnSubmit -= OnSubmit;
        _view.OnCancel -= OnCancel;
        _view.OnRandom -= OnRandom;
        _view.OnHoverIndexChanged -= OnHover;
    }

    // ====== ������ ���� ======
    void OnViewReady()
    {
        EnterCharacterPhase();
    }

    void EnterCharacterPhase()
    {
        _phase = Phase.NavCharacters;
        _p1CharId = _p2CharId = _stageId = null;

        // 1) ������ ���ε�
        _view.SetGridItems(_model.Characters, columns);

        // 2) ������Ʈ�� ������ �� �׺�����Ϳ� ����
        _navigator.SetSnapshot(_view.GetCurrentGridSnapshot());

        // 3) �ʱ� ��Ŀ��(ù ��° ��� �׸�)
        _focusP1 = FindFirstUnlockedId(_model.Characters);
        _focusP2 = _focusP1;

        _view.SetFocus(PlayerId.P1, _focusP1);
        _view.SetFocus(PlayerId.P2, _focusP2);

        // 4) ��/���� ǥ��
        ApplyLockHiddenMarks(_model.Characters);

        // 5) Ǫ��/Ÿ�̸�
        _view.ShowFooter("�̵�: D-Pad/��ƽ  ��  Ȯ��: A  ��  ���: B  ��  ����: Y");
        ResetTimerIfNeeded(useCharacterTimeout, characterTimeoutSec);
    }

    void EnterStagePhase()
    {
        _phase = Phase.NavStages;

        _view.SetGridItems(_model.Stages, columns);
        _navigator.SetSnapshot(_view.GetCurrentGridSnapshot());

        _focusP1 = FindFirstUnlockedId(_model.Stages);
        _view.SetFocus(PlayerId.P1, _focusP1);

        ApplyLockHiddenMarks(_model.Stages);

        _view.ShowFooter("�������� ����  ��  �̵�/Ȯ��/���/����");
        ResetTimerIfNeeded(useStageTimeout, stageTimeoutSec);
    }

    void ResetTimerIfNeeded(bool use, float seconds)
    {
        if (use && seconds > 0f)
        {
            _timer = seconds;
            _view.ShowTimer(_timer);
        }
        else
        {
            _timer = -1f;
            _view.ShowTimer(0f);
        }
    }

    void Update()
    {
        // ������ Ÿ�̸� ó��(�ʿ��� ����)
        if (_timer > 0f)
        {
            _timer -= Time.deltaTime;
            _view.ShowTimer(Mathf.Max(0f, _timer));
            if (_timer <= 0f)
                AutoConfirmByTimeout();
        }
    }

    void AutoConfirmByTimeout()
    {
        switch (_phase)
        {
            case Phase.NavCharacters:
                // ���� ��Ŀ���� �״�� Ȯ��(���̸� ù ������� ����)
                var curId = _focusP1;
                var curItem = FindItemById(_model.Characters, curId);
                if (curItem == null || !_model.IsUnlocked(curItem.Id))
                    curId = FindFirstUnlockedId(_model.Characters);
                ConfirmCharacter(PlayerId.P1, curId);
                ConfirmCharacter(PlayerId.P2, curId); // ���� �����̸� ��Ģ�� ���� �ٸ��� ó�� ����
                if (_p1CharId != null && _p2CharId != null) EnterStagePhase();
                break;

            case Phase.NavStages:
                var stId = _focusP1;
                var stItem = FindItemById(_model.Stages, stId);
                if (stItem == null || !_model.IsUnlocked(stItem.Id))
                    stId = FindFirstUnlockedId(_model.Stages);
                ConfirmStage(stId);
                if (_stageId != null) Finish();
                break;
        }
    }

    // ====== �Է� ó�� ======
    void OnNavigate(Vector2Int dir, PlayerId pid)
    {
        if (_phase is not (Phase.NavCharacters or Phase.NavStages)) return;

        int current = pid == PlayerId.P1 ? _focusP1 : _focusP2;
        var v = new Vector2(dir.x, dir.y);
        if (_navigator.TryNavigate(current, v, out var next))
        {
            if (pid == PlayerId.P1) _focusP1 = next; else _focusP2 = next;
            _view.SetFocus(pid, next);
            _view.Blink(next);
            _view.PlaySfx(SfxTag.Move);

            // ������ �г� ������Ʈ
            var item = (_phase == Phase.NavCharacters)
                ? FindItemById(_model.Characters, next)
                : FindItemById(_model.Stages, next);
            if (item != null) _view.ShowDetails(new DetailViewData(item.DisplayName));
        }
    }

    void OnHover(int id, PlayerId pid)
    {
        if (_phase != Phase.NavCharacters) return;
        var item = FindItemById(_model.Characters, id);
        if (item != null) _view.ShowDetails(new DetailViewData(item.DisplayName));
    }

    void OnSubmit(PlayerId pid)
    {
        switch (_phase)
        {
            case Phase.NavCharacters:
                var id = pid == PlayerId.P1 ? _focusP1 : _focusP2;
                ConfirmCharacter(pid, id);
                if (_p1CharId != null && _p2CharId != null) EnterStagePhase();
                break;

            case Phase.NavStages:
                ConfirmStage(_focusP1); // ���������� 1�� ��Ŀ���� ���
                if (_stageId != null) Finish();
                break;
        }
    }

    void OnCancel(PlayerId pid)
    {
        switch (_phase)
        {
            case Phase.NavStages:
                _stageId = null;
                _phase = Phase.ConfirmedCharacters;
                _view.ShowFooter("�������� �ٽ� ����");
                _view.PlaySfx(SfxTag.Cancel);
                EnterStagePhase(); // ������ ������
                break;
            case Phase.NavCharacters:
                // �ʿ� �� Ÿ��Ʋ�� ���� ��
                _view.PlaySfx(SfxTag.Cancel);
                break;
        }
    }

    void OnRandom(PlayerId pid)
    {
        if (_phase == Phase.NavCharacters)
        {
            var idx = PickRandom(_model.Characters, _model.IsUnlocked);
            var id = NodeIdOfIndex(idx); // �� ��� id�� 1:1�� �ƴ� �� �����Ƿ� ���� ������Ʈ �Ծ࿡ �°� ����
            ConfirmCharacter(pid, id);
            _view.PlaySfx(SfxTag.Confirm);
            if (_p1CharId != null && _p2CharId != null) EnterStagePhase();
        }
        else if (_phase == Phase.NavStages)
        {
            var idx = PickRandom(_model.Stages, _model.IsUnlocked);
            var id = NodeIdOfIndex(idx);
            ConfirmStage(id);
            _view.PlaySfx(SfxTag.Confirm);
            if (_stageId != null) Finish();
        }
    }

    // ====== ����/Ȯ�� ======
    void ConfirmCharacter(PlayerId pid, int nodeId)
    {
        var item = FindItemById(_model.Characters, nodeId);
        if (item == null || !_model.IsUnlocked(item.Id))
        {
            _view.PlaySfx(SfxTag.Error);
            return;
        }

        // �ߺ� ���� ��å(���ϸ� ������� �ٲ㵵 ��)
        if (pid == PlayerId.P2 && _p1CharId == item.Id)
        {
            _view.PlaySfx(SfxTag.Error);
            return;
        }
        if (pid == PlayerId.P1 && _p2CharId == item.Id)
        {
            _view.PlaySfx(SfxTag.Error);
            return;
        }

        if (pid == PlayerId.P1) _p1CharId = item.Id; else _p2CharId = item.Id;
        _view.PlaySfx(SfxTag.Confirm);
    }

    void ConfirmStage(int nodeId)
    {
        var item = FindItemById(_model.Stages, nodeId);
        if (item == null || !_model.IsUnlocked(item.Id))
        {
            _view.PlaySfx(SfxTag.Error);
            return;
        }
        _stageId = item.Id;
        _view.PlaySfx(SfxTag.Confirm);
    }

    void Finish()
    {
        _phase = Phase.TransitionOut;
        var result = new SelectionResult(_p1CharId, _p2CharId, _stageId);
        SelectionBroker.Commit(result); // ���� ���Ŀ�� ���� ���� ����
        // �� �ε� Ʈ���Ŵ� ������Ʈ ���δ��� �°� ȣ��
    }

    // ====== ���� ======
    int FindFirstUnlockedId(IReadOnlyList<SelectableItemViewData> items)
    {
        var snapshot = _view.GetCurrentGridSnapshot();
        foreach (var n in snapshot)
        {
            var item = items.FirstOrDefault(x => x.Id == FindItemIdByNode(n.id, items));
            if (item != null && _model.IsUnlocked(item.Id) && !n.hidden && n.interactable && !n.locked)
                return n.id;
        }
        // ����: ù ���
        return snapshot.Count > 0 ? snapshot[0].id : 0;
    }

    void ApplyLockHiddenMarks(IReadOnlyList<SelectableItemViewData> items)
    {
        var snapshot = _view.GetCurrentGridSnapshot();
        foreach (var n in snapshot)
        {
            var id = FindItemIdByNode(n.id, items); // �Ծ࿡ ���� ����
            bool locked = id == null || !_model.IsUnlocked(id);
            bool hidden = id == null || _model.IsHidden(id);
            _view.SetLocked(n.id, locked, hidden);
        }
    }

    SelectableItemViewData FindItemById(IReadOnlyList<SelectableItemViewData> items, int nodeId)
    {
        var itemId = FindItemIdByNode(nodeId, items);
        if (itemId == null) return null;
        return items.FirstOrDefault(x => x.Id == itemId);
    }

    // === ��� id �� ������ Id ���� �Ծ� ===
    // ����: NodeGeom.id�� items �ε����� ����Ű��, �� �ε����� ViewData.Id�� ���� ���ڿ� ID��� ����.
    // ������Ʈ�� �ٸ��� �� �κи� View �� �Ծ࿡ �°� ��ü�ϸ� �ȴ�.
    string FindItemIdByNode(int nodeId, IReadOnlyList<SelectableItemViewData> items)
    {
        int idx = NodeIndexFromId(nodeId);
        if ((uint)idx >= (uint)items.Count) return null;
        return items[idx].Id;
    }

    // Node id �� index: �⺻ ������ ��id == index����� ���� (View�� ����)
    // �ٸ� �Ծ��̸� View �ʿ� ���� �ΰ� �����ϵ��� �ٲ��.
    int NodeIndexFromId(int nodeId) => nodeId;

    int NodeIdOfIndex(int index) => index;

    int PickRandom(IReadOnlyList<SelectableItemViewData> list, Func<string, bool> unlocked)
    {
        var pool = new List<int>(list.Count);
        for (int i = 0; i < list.Count; i++)
            if (unlocked(list[i].Id)) pool.Add(i);
        if (pool.Count == 0) return 0;
        return pool[UnityEngine.Random.Range(0, pool.Count)];
    }
}

/// <summary> ���� ������ ��� ���Ŀ(�� �� ���޿�) </summary>
public static class SelectionBroker
{
    public static SelectionResult Last;
    public static void Commit(SelectionResult r) => Last = r;
}
