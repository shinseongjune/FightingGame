using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// - 페이즈 관리, 검증(락/중복), 타이머, 결과 커밋, 뷰 바인딩/피드백
/// - 네비게이션(다음 포커스 결정)은 GeometryNavigator에게 위임
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

    // 포커스/선택 상태
    int _focusP1, _focusP2;               // 현재 포커스 id (NodeGeom.id)
    string _p1CharId, _p2CharId, _stageId;

    float _timer;                          // 현재 페이즈의 남은 시간(옵션)

    // ====== 외부 초기화 ======
    public void Initialize(ISelectSceneView view, ISelectSceneModel model)
    {
        _view = view;
        _model = model;

        // 이벤트 배선
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

    // ====== 페이즈 진입 ======
    void OnViewReady()
    {
        EnterCharacterPhase();
    }

    void EnterCharacterPhase()
    {
        _phase = Phase.NavCharacters;
        _p1CharId = _p2CharId = _stageId = null;

        // 1) 데이터 바인딩
        _view.SetGridItems(_model.Characters, columns);

        // 2) 지오메트리 스냅샷 → 네비게이터에 전달
        _navigator.SetSnapshot(_view.GetCurrentGridSnapshot());

        // 3) 초기 포커스(첫 번째 언락 항목)
        _focusP1 = FindFirstUnlockedId(_model.Characters);
        _focusP2 = _focusP1;

        _view.SetFocus(PlayerId.P1, _focusP1);
        _view.SetFocus(PlayerId.P2, _focusP2);

        // 4) 락/히든 표시
        ApplyLockHiddenMarks(_model.Characters);

        // 5) 푸터/타이머
        _view.ShowFooter("이동: D-Pad/스틱  ·  확정: A  ·  취소: B  ·  랜덤: Y");
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

        _view.ShowFooter("스테이지 선택  ·  이동/확정/취소/랜덤");
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
        // 간단한 타이머 처리(필요할 때만)
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
                // 현재 포커스를 그대로 확정(락이면 첫 언락으로 폴백)
                var curId = _focusP1;
                var curItem = FindItemById(_model.Characters, curId);
                if (curItem == null || !_model.IsUnlocked(curItem.Id))
                    curId = FindFirstUnlockedId(_model.Characters);
                ConfirmCharacter(PlayerId.P1, curId);
                ConfirmCharacter(PlayerId.P2, curId); // 단일 선택이면 규칙에 따라 다르게 처리 가능
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

    // ====== 입력 처리 ======
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

            // 디테일 패널 업데이트
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
                ConfirmStage(_focusP1); // 스테이지는 1개 포커스만 사용
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
                _view.ShowFooter("스테이지 다시 선택");
                _view.PlaySfx(SfxTag.Cancel);
                EnterStagePhase(); // 간단히 재진입
                break;
            case Phase.NavCharacters:
                // 필요 시 타이틀로 복귀 등
                _view.PlaySfx(SfxTag.Cancel);
                break;
        }
    }

    void OnRandom(PlayerId pid)
    {
        if (_phase == Phase.NavCharacters)
        {
            var idx = PickRandom(_model.Characters, _model.IsUnlocked);
            var id = NodeIdOfIndex(idx); // 뷰 노드 id와 1:1이 아닐 수 있으므로 실제 프로젝트 규약에 맞게 매핑
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

    // ====== 검증/확정 ======
    void ConfirmCharacter(PlayerId pid, int nodeId)
    {
        var item = FindItemById(_model.Characters, nodeId);
        if (item == null || !_model.IsUnlocked(item.Id))
        {
            _view.PlaySfx(SfxTag.Error);
            return;
        }

        // 중복 금지 정책(원하면 허용으로 바꿔도 됨)
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
        SelectionBroker.Commit(result); // 간단 브로커로 다음 씬에 전달
        // 씬 로드 트리거는 프로젝트 씬로더에 맞게 호출
    }

    // ====== 헬퍼 ======
    int FindFirstUnlockedId(IReadOnlyList<SelectableItemViewData> items)
    {
        var snapshot = _view.GetCurrentGridSnapshot();
        foreach (var n in snapshot)
        {
            var item = items.FirstOrDefault(x => x.Id == FindItemIdByNode(n.id, items));
            if (item != null && _model.IsUnlocked(item.Id) && !n.hidden && n.interactable && !n.locked)
                return n.id;
        }
        // 폴백: 첫 노드
        return snapshot.Count > 0 ? snapshot[0].id : 0;
    }

    void ApplyLockHiddenMarks(IReadOnlyList<SelectableItemViewData> items)
    {
        var snapshot = _view.GetCurrentGridSnapshot();
        foreach (var n in snapshot)
        {
            var id = FindItemIdByNode(n.id, items); // 규약에 따라 매핑
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

    // === 노드 id ↔ 아이템 Id 매핑 규약 ===
    // 샘플: NodeGeom.id가 items 인덱스를 가리키고, 그 인덱스의 ViewData.Id가 실제 문자열 ID라고 가정.
    // 프로젝트가 다르면 이 부분만 View 쪽 규약에 맞게 교체하면 된다.
    string FindItemIdByNode(int nodeId, IReadOnlyList<SelectableItemViewData> items)
    {
        int idx = NodeIndexFromId(nodeId);
        if ((uint)idx >= (uint)items.Count) return null;
        return items[idx].Id;
    }

    // Node id → index: 기본 구현은 “id == index”라고 가정 (View가 보장)
    // 다른 규약이면 View 쪽에 맵을 두고 질의하도록 바꿔라.
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

/// <summary> 가장 간단한 결과 브로커(씬 간 전달용) </summary>
public static class SelectionBroker
{
    public static SelectionResult Last;
    public static void Commit(SelectionResult r) => Last = r;
}
