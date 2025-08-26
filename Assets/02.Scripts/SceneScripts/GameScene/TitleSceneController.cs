using UnityEngine;

public class TitleSceneController : MonoBehaviour
{
    public void OnClick_StartGame()
    {
        SceneLoader.Instance.LoadScene("SelectScene");
    }

    public void OnClick_ExitGame()
    {
        Application.Quit();
    }
}
