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

    // 슬롯(누가 눌렀는지)을 함께 전달
    public event Action<PlayerSlotId> OnRematchClicked;
    public event Action<PlayerSlotId> OnSelectClicked;
    public event Action<PlayerSlotId> OnTitleClicked;

    // Controller가 현재 조작 주체를 알려줌 (로컬 PvP면 키/패드로 번갈아가며?)
    private PlayerSlotId currentControlSlot = PlayerSlotId.P1;

    void Awake()
    {
        // 기본적으로 내(=currentControlSlot) 선택 버튼을 누른다고 가정
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

    // --- 내 선택 하이라이트 ---
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

    // --- 상대 선택 하이라이트 ---
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

    // 모두 끄기 유틸
    public void ClearAllHighlights()
    {
        ShowMyRematch(false); ShowMySelect(false); ShowMyTitle(false);
        ShowOppRematch(false); ShowOppSelect(false); ShowOppTitle(false);
    }
}
