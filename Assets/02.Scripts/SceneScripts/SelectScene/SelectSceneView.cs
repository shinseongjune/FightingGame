using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum SelectSceneSFXTag
{
    Submit,
    Cancel,
    Navigate,
}

public class SelectSceneView : MonoBehaviour
{
    [Header("Grid Roots (UI Containers)")]
    public RectTransform characterGridRoot;
    public RectTransform stageGridRoot;

    [Header("Grid Cell Prefabs")]
    [SerializeField] private GameObject prefab_CharacterCell;
    [SerializeField] private GameObject prefab_StageCell;

    [Tooltip("Grid")]
    private List<GameObject> _characterGrid = new List<GameObject>();
    private List<GameObject> _stageGrid = new List<GameObject>();
    private List<GameObject> _currentGrid;

    [Header("Focus Images")]
    [SerializeField] private Image img_Focus_p1;
    [SerializeField] private Image img_Focus_p2;

    private int currentIdx;

    //TODO: vfx, sfx

    // ------------- Controller ���� �̺�Ʈ --------------
    public event Action<Vector2, int> OnNavigate;
    public event Action<int> OnSubmit;
    public event Action<int> OnCancel;
    public event Action<int> OnRandom;
    public event Action<int, int> OnHoverIndexChanged;
    public event Action OnViewReady;

    // ------------- �׸��� ó�� ------------
    public void BuildCharacterGrid(IReadOnlyList<CharacterData> data)
    {
        ClearGrid(_characterGrid);
        for (int i = 0; i < data.Count; i++)
        {
            var c = data[i];
            var go = Instantiate(prefab_CharacterCell, characterGridRoot);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = c.gridPos;

            // TODO: Addressables�� �̹��� �ε��ؼ� �� UI ���ε�
            // var cell = go.GetComponent<CharacterCell>();
            // cell.SetData(c);

            _characterGrid.Add(go);
        }
    }

    public void BuildStageGrid(IReadOnlyList<StageData> data)
    {
        ClearGrid(_stageGrid);
        for (int i = 0; i < data.Count; i++)
        {
            var s = data[i];
            var go = Instantiate(prefab_StageCell, stageGridRoot);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = s.gridPos;

            // TODO: Addressables�� �̹��� �ε��ؼ� �� UI ���ε�
            // var cell = go.GetComponent<StageCell>();
            // cell.SetData(s);

            _stageGrid.Add(go);
        }
    }

    void ClearGrid(List<GameObject> list)
    {
        for (int i = 0; i < list.Count; i++) Destroy(list[i]);
        list.Clear();
    }

    public void SetCharacterGridOn()
    {
        SetActive(_stageGrid, false);
        SetActive(_characterGrid, true);
        _currentGrid = _characterGrid;
        // TODO: �Ϸ���Ʈ/������ ����
    }

    public void SetStageGridOn()
    {
        SetActive(_characterGrid, false);
        SetActive(_stageGrid, true);
        _currentGrid = _stageGrid;
        // TODO: ������/������ ����
    }

    void SetActive(List<GameObject> list, bool active)
    {
        for (int i = 0; i < list.Count; i++) list[i].SetActive(active);
    }

    // ------------- Render ��� --------------
    public void InitDone() => OnViewReady?.Invoke();

    public void SetFocus(int playerId, int idx)
    {
        if (_currentGrid == null || _currentGrid.Count == 0) return;
        idx = Mathf.Clamp(idx, 0, _currentGrid.Count - 1);

        Image focus = playerId switch
        {
            0 => img_Focus_p1,
            1 => img_Focus_p2,
            _ => img_Focus_p1,
        };
        focus.rectTransform.anchoredPosition = _currentGrid[idx].GetComponent<RectTransform>().anchoredPosition;

        currentIdx = idx;

        //TODO: �Ҹ� ���
        //TODO: ĳ���� �Ϸ���Ʈ, ������, �������� ������ �� ǥ��
        OnHoverIndexChanged?.Invoke(playerId, idx);
    }

    //public void ShowDetails()
    //{
    //
    //}
    //
    //public void ShowFooter()
    //{
    //
    //}

    //public void ShowTimer()
    //{
    //
    //}

    //public void PlaySfx(SelectSceneSFXTag tag)
    //{
    //
    //}

    //public void SetStagePreview()
    //{
    //
    //}
}
