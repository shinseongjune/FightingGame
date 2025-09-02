using UnityEngine;
using UnityEngine.UI;

public class CharacterCell : MonoBehaviour
{
    [HideInInspector] public Image background;
    [HideInInspector] public Image headerImg;

    //TODO: 일러스트 등 이미지 로드해서 등록.

    private void Awake()
    {
        background = GetComponent<Image>();
        headerImg = transform.GetChild(0).GetComponent<Image>();
    }
}
