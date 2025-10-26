using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SABar : MonoBehaviour
{
    private Slider slider;
    private TextMeshProUGUI text;

    private CharacterProperty character;

    public void SetCharacter(CharacterProperty chara)
    {
        character = chara;
    }

    private void Start()
    {
        slider = GetComponent<Slider>();
        text = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void Update()
    {
        if (character != null && slider != null)
        {
            int textValue = Mathf.FloorToInt(character.saGauge / 100);
            text.text = $"{textValue:0}";
            if (textValue == 3)
            {
                slider.value = 1f;
            }
            else
            {
                slider.value = character.saGauge % 100 / 100;
            }
        }
    }
}
