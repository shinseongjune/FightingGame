using UnityEngine;
using UnityEngine.UI;

public class StageCell : MonoBehaviour
{
    [SerializeField] private Image headerImage;
    [SerializeField] private GameObject lockBadge;

    private StageData data;

    public void SetData(StageData d)
    {
        data = d;
        lockBadge?.SetActive(d.isLocked);

        // PortraitLibrary에서 가져오기
        var sprite = PortraitLibrary.Instance.Get($"Portrait/{d.stageName}");
        if (sprite != null)
            headerImage.sprite = sprite;
        else
            Debug.LogWarning($"[Cell] Portrait not found: {d.stageName}");
    }

    public StageData GetData() => data;
}
