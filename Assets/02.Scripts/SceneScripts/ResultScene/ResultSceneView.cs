using System;
using UnityEngine;
using UnityEngine.UI;

public class ResultSceneView : MonoBehaviour
{
    private GameManager _gm;

    [SerializeField] Button button_Rematch;
    [SerializeField] Button button_Select;
    [SerializeField] Button button_Title;

    Action<UserData> OnRematch;
    Action<UserData> OnSelect;
    Action<UserData> OnTitle;

    private void Awake()
    {
        _gm = GameManager.Instance;
    }

    public void Btn_Rematch()
    {
        ActivateButtons(false);

        OnRematch?.Invoke(_gm.currentUser);
    }

    public void Btn_Select()
    {
        ActivateButtons(false);

        OnSelect?.Invoke(_gm.currentUser);
    }

    public void Btn_Title()
    {
        ActivateButtons(false);
        OnTitle?.Invoke(_gm.currentUser);

        SceneLoader.Instance.LoadScene("TitleScene");
    }

    public void ActivateButtons(bool active)
    {
        button_Rematch.interactable = active;
        button_Select.interactable = active;
        button_Title.interactable = active;
    }
}
