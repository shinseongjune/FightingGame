using UnityEngine;
using UnityEngine.UI;

public class CharacterIllust : MonoBehaviour
{
    [SerializeField] private Image illustImage;

    public void ShowIllust(string characterName)
    {
        var sprite = IllustrationLibrary.Instance.Get($"Illust/{characterName}");
        if (sprite != null)
            illustImage.sprite = sprite;
        else
            Debug.LogWarning($"[Illust] Not found: {characterName}");
    }
}