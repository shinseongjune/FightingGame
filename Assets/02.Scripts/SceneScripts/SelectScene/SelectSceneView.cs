using System;
using UnityEngine;
using UnityEngine.UI;

public struct CharacterData
{
    public string addressableId;
    public string headerImgName;
    public string illustName;
    public string characterName;
    public Vector2 gridPos;
    public bool isLocked;
}

public struct StageData
{
    public string addressableId;
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

    // ------------- Render 명령 --------------
    public void MakeCharacterGrid()
    {

    }

    public void MakeStageGrid()
    {

    }

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
