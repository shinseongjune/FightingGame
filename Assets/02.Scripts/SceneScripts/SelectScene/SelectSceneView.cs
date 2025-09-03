using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public struct CharacterData
{
    public int id;
    public string addressableName;
    public string headerImgName;
    public string illustName;
    public string characterName;
    public Vector2 gridPos;
    public bool isLocked;
}

public struct StageData
{
    public int id;
    public string addressableName;
    public string headerImgName;
    public string illustName;
    public string stageName;
    public Vector2 gridPos;
    public bool isLocked;
}

public enum SelectSceneSFXTag
{
    Submit,
    Cancel,
    Navigate,
}

public class SelectSceneView : MonoBehaviour
{
    [Tooltip("Grid Roots (UI Containers)")]
    public RectTransform characterGridRoot;
    public RectTransform stageGridRoot;

    [Header("Grid Cell Prefabs")]
    [SerializeField] private GameObject prefab_CharacterCell;
    [SerializeField] private GameObject prefab_StageCell;

    [Header("Grid Datas")]
    [SerializeField] private CharacterData[] characters;
    [SerializeField] private StageData[] stages;

    [Tooltip("Grid")]
    private List<GameObject> _characterGrid = new List<GameObject>();
    private List<GameObject> _stageGrid = new List<GameObject>();
    private List<GameObject> _currentGrid = null;

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
    public void MakeCharacterGrid()
    {
        for (int i = 0; i < _characterGrid.Count; i++)
        {
            GameObject cell = _characterGrid[i];
            Destroy(cell);
        }
        _characterGrid.Clear();

        foreach (var character in characters)
        {
            GameObject go = Instantiate(prefab_CharacterCell, characterGridRoot);
            go.GetComponent<RectTransform>().anchoredPosition = character.gridPos;
            CharacterCell cell = go.GetComponent<CharacterCell>();

            //TODO: cell.background, cell.headerImg�� addressable���� �ε��� �̹��� �ְ� ������ �ֱ�.
        }
    }

    public void MakeStageGrid()
    {
        for (int i = 0; i < _stageGrid.Count; i++)
        {
            GameObject cell = _stageGrid[i];
            Destroy(cell);
        }
        _stageGrid.Clear();

        foreach (var stage in stages)
        {
            GameObject go = Instantiate(prefab_StageCell, stageGridRoot);
            go.GetComponent<RectTransform>().anchoredPosition= stage.gridPos;
            StageCell cell = go.GetComponent<StageCell>();

            //TODO: background, headerImg�� addressable���� �ε��� �̹��� �ְ� ������ �ֱ�.
        }
    }

    public void SetCharacterGridOn()
    {
        for (int i = 0; i < _stageGrid.Count; i++)
        {
            _stageGrid[i].SetActive(false);
        }

        for (int i = 0; i < _characterGrid.Count; i++)
        {
            _characterGrid[i].SetActive(true);
        }

        _currentGrid = _characterGrid;

        // setfocus, �Ϸ���Ʈ, ������
    }

    public void SetStageGridOn()
    {
        for (int i = 0; i < _characterGrid.Count; i++)
        {
            _characterGrid[i].SetActive(false);
        }

        for (int i = 0; i < _stageGrid.Count; i++)
        {
            _stageGrid[i].SetActive(true);
        }

        _currentGrid = _stageGrid;

        // setfocus, �Ϸ���Ʈ, ������
    }

    // ------------- Render ��� --------------
    public void Init()
    {
        MakeCharacterGrid();
        MakeStageGrid();
        SetCharacterGridOn();


    }

    public void SetFocus(int playerId, int idx)
    {
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
