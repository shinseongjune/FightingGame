using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public enum SelectSceneSFXTag
{
    Submit,
    Cancel,
    Navigate,
}

public class SelectSceneView : MonoBehaviour
{
    [Header("Illust UI")]
    [SerializeField] private Image characterIllust; // Image_Character Illust
    [SerializeField] private Image stageIllust;     // Image_Stage Illust

    [SerializeField] private Sprite placeholder;    // 기본 표시(없으면 null)

    private string lastCharacterKey;
    private string lastStageKey;

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

    // ------------- Controller 전달 이벤트 --------------
    public event Action<Vector2, int> OnNavigate;
    public event Action<int> OnSubmit;
    public event Action<int> OnCancel;
    public event Action<int> OnRandom;
    public event Action<int, int> OnHoverIndexChanged;
    public event Action OnViewReady;

    private void Awake()
    {
        if (characterIllust) characterIllust.preserveAspect = true;
        if (stageIllust) stageIllust.preserveAspect = true;

        if (placeholder)
        {
            if (characterIllust && characterIllust.sprite == null)
                characterIllust.sprite = placeholder;
            if (stageIllust && stageIllust.sprite == null)
                stageIllust.sprite = placeholder;
        }
    }

    // --- 캐릭터 셀 포커스/선택 시 호출 ---
    public void ShowCharacterIllust(CharacterData d)
    {
        string key = $"Illust/{d.characterName}";
        if (key == lastCharacterKey) return;
        lastCharacterKey = key;

        var sprite = IllustrationLibrary.Instance.Get(key);
        if (sprite != null) characterIllust.sprite = sprite;
        else
        {
            if (placeholder) characterIllust.sprite = placeholder;
            Debug.LogWarning($"[View] Character illust not found: {key}");
        }
    }

    // --- 스테이지 셀 포커스/선택 시 호출 ---
    public void ShowStageIllust(StageData d)
    {
        string key = $"StageIllust/{d.stageName}";
        if (key == lastStageKey) return;
        lastStageKey = key;

        var sprite = IllustrationLibrary.Instance.Get(key);
        if (sprite != null) stageIllust.sprite = sprite;
        else
        {
            if (placeholder) stageIllust.sprite = placeholder;
            Debug.LogWarning($"[View] Stage illust not found: {key}");
        }
    }

    // ------------- 그리드 처리 ------------
    public void BuildCharacterGrid(IReadOnlyList<CharacterData> data)
    {
        ClearGrid(_characterGrid);
        for (int i = 0; i < data.Count; i++)
        {
            var c = data[i];
            var go = Instantiate(prefab_CharacterCell, characterGridRoot);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = c.gridPos;

            var cell = go.GetComponent<CharacterCell>();
            cell.SetData(c);

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

            var cell = go.GetComponent<StageCell>();
            cell.SetData(s);

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

        // --- 일러스트 전환 처리 ---
        if (characterIllust) characterIllust.gameObject.SetActive(true);
        if (stageIllust) stageIllust.gameObject.SetActive(false);

        // 같은 키로 스킵되는 걸 방지(강제 갱신)
        lastCharacterKey = null;

        // 전환 직후에는 SetFocus만 호출해서 일러스트도 같은 경로로 그리게 한다
        if (_currentGrid.Count > 0) SetFocus(0, 0);
    }

    public void SetStageGridOn()
    {
        SetActive(_characterGrid, false);
        SetActive(_stageGrid, true);
        _currentGrid = _stageGrid;

        // --- 일러스트 전환 처리 ---
        if (characterIllust) characterIllust.gameObject.SetActive(false);
        if (stageIllust) stageIllust.gameObject.SetActive(true);

        lastStageKey = null;

        if (_currentGrid.Count > 0) SetFocus(0, 0);
    }

    void SetActive(List<GameObject> list, bool active)
    {
        for (int i = 0; i < list.Count; i++) list[i].SetActive(active);
    }

    // ------------- Render 명령 --------------
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

        // --- 일러스트 갱신 ---
        if (ReferenceEquals(_currentGrid, _characterGrid))
        {
            var cell = _currentGrid[idx].GetComponent<CharacterCell>();
            if (cell != null) ShowCharacterIllust(cell.GetData());
        }
        else if (ReferenceEquals(_currentGrid, _stageGrid))
        {
            var cell = _currentGrid[idx].GetComponent<StageCell>();
            if (cell != null) ShowStageIllust(cell.GetData());
        }

        // TODO: 소리 재생

        OnHoverIndexChanged?.Invoke(playerId, idx);
    }

    public void SetFocusVisible(int playerId, bool visible)
    {
        var img = (playerId == 0) ? img_Focus_p1 : img_Focus_p2;
        if (img != null) img.gameObject.SetActive(visible);
    }

    public void SetFocusVisible(bool p1Visible, bool p2Visible)
    {
        SetFocusVisible(0, p1Visible);
        SetFocusVisible(1, p2Visible);
    }
}
