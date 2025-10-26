using UnityEngine;
using TMPro;
using System.Collections;

public class Combos : MonoBehaviour
{
    private CharacterProperty property;
    private TextMeshProUGUI text;

    private void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    public void Init()
    {
        property.OnComboChanged += UpdateCombo;
        text.color = new Color(text.color.r, text.color.g, text.color.b, 0f);
    }

    public void BindCharacter(CharacterProperty character)
    {
        property = character;
    }

    public void UpdateCombo()
    {
        if (property == null || text == null) return;
        int comboCount = property.currentComboCount;

        if (comboCount < 2) return;

        text.text = $"{comboCount} Hits";

        Show();
    }

    private void Show()
    {
        StopAllCoroutines();
        Color currentColor = text.color;
        currentColor.a = 1f;
        text.color = currentColor;
        StartCoroutine(FadeOut(1.5f));
    }

    private IEnumerator FadeOut(float delay)
    {
        yield return new WaitForSeconds(delay);

        while (text.color.a > 0f)
        {
            Color currentColor = text.color;
            currentColor.a = Mathf.Lerp(currentColor.a, 0, Time.deltaTime * 5f);
            text.color = currentColor;

            yield return null;
        }
    }
}
