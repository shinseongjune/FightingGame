using UnityEngine;
using UnityEngine.UI;

public class CharacterCell : MonoBehaviour
{
    [SerializeField] private Image headerImage;
    [SerializeField] private GameObject lockBadge;

    private CharacterData data;

    public void SetData(CharacterData d)
    {
        data = d;
        lockBadge?.SetActive(d.isLocked);

        // PortraitLibrary에서 가져오기
        var sprite = PortraitLibrary.Instance.Get($"Portrait/{d.characterName}");
        if (sprite != null)
            headerImage.sprite = sprite;
        else
            Debug.LogWarning($"[Cell] Portrait not found: {d.characterName}");
    }

    public CharacterData GetData() => data;
}
