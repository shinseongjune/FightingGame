using System;
using System.Collections.Generic;
using UnityEngine;

public interface ISelectSceneView
{
    // View → Controller (사용자 행위 이벤트)
    event Action OnViewReady;
    event Action<Vector2Int, PlayerId> OnNavigate;
    event Action<PlayerId> OnSubmit;
    event Action<PlayerId> OnCancel;
    event Action<PlayerId> OnRandom;
    event Action<int, PlayerId> OnHoverIndexChanged;

    // Controller → View (렌더 명령)
    void SetGridItems(IReadOnlyList<SelectableItemViewData> items, int columns);
    void SetFocus(PlayerId pid, int id);                         // id는 NodeGeom.id
    void SetLocked(int id, bool locked, bool hidden);
    void ShowDetails(DetailViewData data);
    void ShowFooter(string left, string right = "");
    void ShowTimer(float secondsRemain);
    void PlaySfx(SfxTag tag);
    void Blink(int id);

    // 뷰가 현재 그리드의 지오메트리 스냅샷을 제공
    IReadOnlyList<NodeGeom> GetCurrentGridSnapshot();
}

public enum SfxTag { Move, Confirm, Cancel, Error }
