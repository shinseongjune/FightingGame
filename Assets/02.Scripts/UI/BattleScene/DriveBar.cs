using UnityEngine;
using UnityEngine.UI;

public class DriveBar : MonoBehaviour
{
    private Slider slider;

    private CharacterProperty character;

    public void SetCharacter(CharacterProperty chara)
    {
        character = chara;
    }

    private void Start()
    {
        slider = GetComponent<Slider>();
    }

    public void Update()
    {
        if (character != null && slider != null)
        {
            if (character.isExhausted)
            {
                slider.fillRect.GetComponent<Image>().color = Color.gray;
            }
            else
            {
                slider.fillRect.GetComponent<Image>().color = Color.yellow;
            }

                slider.value = character.driveGauge / character.maxDriveGauge;
        }
    }
}