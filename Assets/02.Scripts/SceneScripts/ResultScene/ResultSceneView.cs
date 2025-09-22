using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultSceneView : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button button_Rematch;
    [SerializeField] private Button button_Select;
    [SerializeField] private Button button_Title;

    [Header("My Choice Highlight")]
    [SerializeField] private GameObject go_RematchOnImage;
    [SerializeField] private GameObject go_SelectOnImage;
    [SerializeField] private GameObject go_TitleOnImage;

    [Header("Opponent Choice Highlight")]
    [SerializeField] private GameObject go_RematchOnImageOpponent;
    [SerializeField] private GameObject go_SelectOnImageOpponent;
    [SerializeField] private GameObject go_TitleOnImageOpponent;

    [Header("Timer")]
    [SerializeField] private TMP_Text tmp_time;

    // ����(���� ��������)�� �Բ� ����
    public event Action<PlayerSlotId> OnRematchClicked;
    public event Action<PlayerSlotId> OnSelectClicked;
    public event Action<PlayerSlotId> OnTitleClicked;

    // Controller�� ���� ���� ��ü�� �˷��� (���� PvP�� Ű/�е�� �����ư���?)
    private PlayerSlotId currentControlSlot = PlayerSlotId.P1;

    void Awake()
    {
        // �⺻������ ��(=currentControlSlot) ���� ��ư�� �����ٰ� ����
        button_Rematch.onClick.AddListener(() => OnRematchClicked?.Invoke(currentControlSlot));
        button_Select.onClick.AddListener(() => OnSelectClicked?.Invoke(currentControlSlot));
        button_Title.onClick.AddListener(() => OnTitleClicked?.Invoke(currentControlSlot));
    }

    public void SetControlSlot(PlayerSlotId slot) => currentControlSlot = slot;

    public void SetCountdown(float seconds)
    {
        if (tmp_time == null) return;
        int s = Mathf.CeilToInt(Mathf.Max(0, seconds));
        if (tmp_time.text != s.ToString()) tmp_time.text = s.ToString();
    }

    // --- �� ���� ���̶���Ʈ ---
    public void ShowMyRematch(bool on)
    {
        if (go_RematchOnImage) go_RematchOnImage.SetActive(on);
    }
    public void ShowMySelect(bool on)
    {
        if (go_SelectOnImage) go_SelectOnImage.SetActive(on);
    }
    public void ShowMyTitle(bool on)
    {
        if (go_TitleOnImage) go_TitleOnImage.SetActive(on);
    }

    // --- ��� ���� ���̶���Ʈ ---
    public void ShowOppRematch(bool on)
    {
        if (go_RematchOnImageOpponent) go_RematchOnImageOpponent.SetActive(on);
    }
    public void ShowOppSelect(bool on)
    {
        if (go_SelectOnImageOpponent) go_SelectOnImageOpponent.SetActive(on);
    }
    public void ShowOppTitle(bool on)
    {
        if (go_TitleOnImageOpponent) go_TitleOnImageOpponent.SetActive(on);
    }

    // ��� ���� ��ƿ
    public void ClearAllHighlights()
    {
        ShowMyRematch(false); ShowMySelect(false); ShowMyTitle(false);
        ShowOppRematch(false); ShowOppSelect(false); ShowOppTitle(false);
    }
}
