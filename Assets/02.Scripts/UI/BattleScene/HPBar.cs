using UnityEngine;
using UnityEngine.UI;

public class HPBar : MonoBehaviour
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
            slider.value = character.hp / character.maxHp;
        }
    }
}
