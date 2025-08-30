// UGUISelectSceneView.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UGUISelectSceneView : MonoBehaviour, ISelectSceneView
{
    [Header("Wiring")]
    [SerializeField] Transform gridRoot;         // 타일들이 붙을 부모
    [SerializeField] GameObject tilePrefab;      // (Button + Image + Text)
    [SerializeField] Image focusP1;              // 포커스 마커 (선택: 이미지)
    [SerializeField] Image focusP2;              // 2P 포커스 마커
    [SerializeField] Text footerLeft, footerRight;
    [SerializeField] Text titleText;             // 상세 패널 등
    [SerializeField] Text timerText;

    // View→Controller
    public event Action OnViewReady;
    public event Action<Vector2Int, PlayerId> OnNavigate;
    public event Action<PlayerId> OnSubmit;
    public event Action<PlayerId> OnCancel;
    public event Action<PlayerId> OnRandom;
    public event Action<int, PlayerId> OnHoverIndexChanged;

    // 내부 상태
    readonly List<RectTransform> _tiles = new();
    readonly List<NodeGeom> _snapshot = new();

    // ==== Controller→View 구현 ====
    public void SetGridItems(IReadOnlyList<SelectableItemViewData> items, int columns)
    {
        // 기존 삭제
        for (int i = gridRoot.childCount - 1; i >= 0; --i)
            Destroy(gridRoot.GetChild(i).gameObject);
        _tiles.Clear();

        for (int i = 0; i < items.Count; i++)
        {
            var go = Instantiate(tilePrefab, gridRoot);
            var rt = (RectTransform)go.transform;
            _tiles.Add(rt);

            // 간단 텍스트 세팅
            var txt = go.GetComponentInChildren<Text>(true);
            if (txt) txt.text = items[i].DisplayName;

            int idx = i; // 캡처
            var btn = go.GetComponent<Button>();
            if (btn) btn.onClick.AddListener(() => OnSubmit?.Invoke(PlayerId.P1));

            // 마우스 호버(선택): 필요 시 EventTrigger로 연결해서 아래 호출
            // OnHoverIndexChanged?.Invoke(idx, PlayerId.P1);
        }

        // 레이아웃 1프레임 후 스냅샷 갱신 위해 코루틴/딜레이 콜
        StartCoroutine(NextFrameReady());
    }

    System.Collections.IEnumerator NextFrameReady()
    {
        yield return null; // 한 프레임 대기(레이아웃 안정화)
        RebuildSnapshot();
        OnViewReady?.Invoke();
    }

    public void SetFocus(PlayerId pid, int id)
    {
        var rt = FindTile(id);
        if (!rt) return;
        var screenPos = WorldToCanvas(rt);
        var target = pid == PlayerId.P1 ? focusP1 : focusP2;
        if (target)
        {
            target.rectTransform.anchoredPosition = screenPos;
            if (!target.gameObject.activeSelf) target.gameObject.SetActive(true);
        }
    }

    public void SetLocked(int id, bool locked, bool hidden)
    {
        var rt = FindTile(id);
        if (!rt) return;
        var cg = rt.GetComponent<CanvasGroup>() ?? rt.gameObject.AddComponent<CanvasGroup>();
        cg.interactable = !locked && !hidden;
        cg.blocksRaycasts = !hidden;
        cg.alpha = hidden ? 0f : (locked ? 0.4f : 1f);
    }

    public void ShowDetails(DetailViewData data)
    {
        if (titleText) titleText.text = data?.Title ?? "";
    }

    public void ShowFooter(string left, string right = "")
    {
        if (footerLeft) footerLeft.text = left ?? "";
        if (footerRight) footerRight.text = right ?? "";
    }

    public void ShowTimer(float secondsRemain)
    {
        if (timerText) timerText.text = (secondsRemain > 0f) ? Mathf.CeilToInt(secondsRemain).ToString() : "";
    }

    public void PlaySfx(SfxTag tag) { /* 오디오 소스 연결해도 되고, 일단 비움 */ }

    public void Blink(int id) { /* 간단 하이라이트 애니메이션 넣고 싶으면 여기 */ }

    public IReadOnlyList<NodeGeom> GetCurrentGridSnapshot() => _snapshot;

    // ==== 스냅샷 빌드 ====
    void RebuildSnapshot()
    {
        _snapshot.Clear();
        for (int i = 0; i < _tiles.Count; i++)
        {
            var rt = _tiles[i];
            _snapshot.Add(new NodeGeom
            {
                id = i, // 규약: id == index
                center = WorldToCanvas(rt),
                size = rt.rect.size,
                locked = false,
                hidden = false,
                interactable = true
            });
        }
    }

    RectTransform FindTile(int id)
    {
        if (id < 0 || id >= _tiles.Count) return null;
        return _tiles[id];
    }

    // 같은 캔버스 기준 좌표로 변환(여기선 anchoredPosition을 그대로 사용)
    Vector2 WorldToCanvas(RectTransform rt)
    {
        // Grid가 동일한 Canvas/RectTransform 계층이면 anchoredPosition이 가장 간단
        return rt.anchoredPosition;
    }

    public void EmitNavigate(Vector2Int dir, PlayerId pid) => OnNavigate?.Invoke(dir, pid);
    public void EmitSubmit(PlayerId pid) => OnSubmit?.Invoke(pid);
    public void EmitCancel(PlayerId pid) => OnCancel?.Invoke(pid);
    public void EmitRandom(PlayerId pid) => OnRandom?.Invoke(pid);
}
