// UGUISelectSceneView.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UGUISelectSceneView : MonoBehaviour, ISelectSceneView
{
    [Header("Wiring")]
    [SerializeField] Transform gridRoot;         // Ÿ�ϵ��� ���� �θ�
    [SerializeField] GameObject tilePrefab;      // (Button + Image + Text)
    [SerializeField] Image focusP1;              // ��Ŀ�� ��Ŀ (����: �̹���)
    [SerializeField] Image focusP2;              // 2P ��Ŀ�� ��Ŀ
    [SerializeField] Text footerLeft, footerRight;
    [SerializeField] Text titleText;             // �� �г� ��
    [SerializeField] Text timerText;

    // View��Controller
    public event Action OnViewReady;
    public event Action<Vector2Int, PlayerId> OnNavigate;
    public event Action<PlayerId> OnSubmit;
    public event Action<PlayerId> OnCancel;
    public event Action<PlayerId> OnRandom;
    public event Action<int, PlayerId> OnHoverIndexChanged;

    // ���� ����
    readonly List<RectTransform> _tiles = new();
    readonly List<NodeGeom> _snapshot = new();

    // ==== Controller��View ���� ====
    public void SetGridItems(IReadOnlyList<SelectableItemViewData> items, int columns)
    {
        // ���� ����
        for (int i = gridRoot.childCount - 1; i >= 0; --i)
            Destroy(gridRoot.GetChild(i).gameObject);
        _tiles.Clear();

        for (int i = 0; i < items.Count; i++)
        {
            var go = Instantiate(tilePrefab, gridRoot);
            var rt = (RectTransform)go.transform;
            _tiles.Add(rt);

            // ���� �ؽ�Ʈ ����
            var txt = go.GetComponentInChildren<Text>(true);
            if (txt) txt.text = items[i].DisplayName;

            int idx = i; // ĸó
            var btn = go.GetComponent<Button>();
            if (btn) btn.onClick.AddListener(() => OnSubmit?.Invoke(PlayerId.P1));

            // ���콺 ȣ��(����): �ʿ� �� EventTrigger�� �����ؼ� �Ʒ� ȣ��
            // OnHoverIndexChanged?.Invoke(idx, PlayerId.P1);
        }

        // ���̾ƿ� 1������ �� ������ ���� ���� �ڷ�ƾ/������ ��
        StartCoroutine(NextFrameReady());
    }

    System.Collections.IEnumerator NextFrameReady()
    {
        yield return null; // �� ������ ���(���̾ƿ� ����ȭ)
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

    public void PlaySfx(SfxTag tag) { /* ����� �ҽ� �����ص� �ǰ�, �ϴ� ��� */ }

    public void Blink(int id) { /* ���� ���̶���Ʈ �ִϸ��̼� �ְ� ������ ���� */ }

    public IReadOnlyList<NodeGeom> GetCurrentGridSnapshot() => _snapshot;

    // ==== ������ ���� ====
    void RebuildSnapshot()
    {
        _snapshot.Clear();
        for (int i = 0; i < _tiles.Count; i++)
        {
            var rt = _tiles[i];
            _snapshot.Add(new NodeGeom
            {
                id = i, // �Ծ�: id == index
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

    // ���� ĵ���� ���� ��ǥ�� ��ȯ(���⼱ anchoredPosition�� �״�� ���)
    Vector2 WorldToCanvas(RectTransform rt)
    {
        // Grid�� ������ Canvas/RectTransform �����̸� anchoredPosition�� ���� ����
        return rt.anchoredPosition;
    }

    public void EmitNavigate(Vector2Int dir, PlayerId pid) => OnNavigate?.Invoke(dir, pid);
    public void EmitSubmit(PlayerId pid) => OnSubmit?.Invoke(pid);
    public void EmitCancel(PlayerId pid) => OnCancel?.Invoke(pid);
    public void EmitRandom(PlayerId pid) => OnRandom?.Invoke(pid);
}
