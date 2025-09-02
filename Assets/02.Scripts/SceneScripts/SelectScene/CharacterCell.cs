using UnityEngine;
using UnityEngine.UI;

public class CharacterCell : MonoBehaviour
{
    [HideInInspector] public Image background;
    [HideInInspector] public Image headerImg;

    //TODO: �Ϸ���Ʈ �� �̹��� �ε��ؼ� ���.

    private void Awake()
    {
        background = GetComponent<Image>();
        headerImg = transform.GetChild(0).GetComponent<Image>();
    }
}
