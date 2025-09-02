using System;
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
    private GameObject[] _characterGrid;
    private GameObject[] _stageGrid;

    [SerializeField] private Image img_Focus;

    //TODO: vfx, sfx

    // ------------- Controller 전달 이벤트 --------------
    public event Action<Vector2, int> OnNavigate;
    public event Action<int> OnSubmit;
    public event Action<int> OnCancel;
    public event Action<int> OnRandom;
    public event Action<int, int> OnHoverIndexChanged;
    public event Action OnViewReady;

    // ------------- 그리드 생성 ------------
    public void MakeCharacterGrid()
    {
        foreach (var character in characters)
        {
            GameObject go = Instantiate(prefab_CharacterCell, characterGridRoot);
            go.GetComponent<RectTransform>().anchoredPosition = character.gridPos;
            CharacterCell cell = go.GetComponent<CharacterCell>();

            //TODO: cell.background, cell.headerImg에 addressable에서 로드한 이미지 넣고 데이터 넣기.
        }
    }

    public void MakeStageGrid()
    {
        foreach (var stage in stages)
        {
            GameObject go = Instantiate(prefab_StageCell, stageGridRoot);
            go.GetComponent<RectTransform>().anchoredPosition= stage.gridPos;
            StageCell cell = go.GetComponent<StageCell>();

            //TODO: background, headerImg에 addressable에서 로드한 이미지 넣고 데이터 넣기.
        }
    }

    // ------------- Render 명령 --------------
    public void SetFocus(int playerId, int idx)
    {

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

    public void ShowTimer()
    {

    }

    //public void PlaySfx(SelectSceneSFXTag tag)
    //{
    //
    //}

    public void Blink(int idx)
    {

    }

    public void SetStagePreview()
    {

    }
}
