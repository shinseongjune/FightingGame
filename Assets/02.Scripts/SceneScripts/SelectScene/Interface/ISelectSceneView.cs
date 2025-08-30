using System;
using System.Collections.Generic;
using UnityEngine;

public interface ISelectSceneView
{
    // View �� Controller (����� ���� �̺�Ʈ)
    event Action OnViewReady;
    event Action<Vector2Int, PlayerId> OnNavigate;
    event Action<PlayerId> OnSubmit;
    event Action<PlayerId> OnCancel;
    event Action<PlayerId> OnRandom;
    event Action<int, PlayerId> OnHoverIndexChanged;

    // Controller �� View (���� ���)
    void SetGridItems(IReadOnlyList<SelectableItemViewData> items, int columns);
    void SetFocus(PlayerId pid, int id);                         // id�� NodeGeom.id
    void SetLocked(int id, bool locked, bool hidden);
    void ShowDetails(DetailViewData data);
    void ShowFooter(string left, string right = "");
    void ShowTimer(float secondsRemain);
    void PlaySfx(SfxTag tag);
    void Blink(int id);

    // �䰡 ���� �׸����� ������Ʈ�� �������� ����
    IReadOnlyList<NodeGeom> GetCurrentGridSnapshot();
}

public enum SfxTag { Move, Confirm, Cancel, Error }
