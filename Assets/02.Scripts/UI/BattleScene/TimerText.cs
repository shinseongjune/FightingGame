using UnityEngine;
using TMPro;

public class TimerText : MonoBehaviour
{
    [SerializeField] RoundController roundController;

    private TextMeshProUGUI timerText;

    private void Awake()
    {
        timerText = GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        if (roundController == null)
        {
            return;
        }

        roundController.OnTimerChanged += UpdateTimerText;
    }

    private void UpdateTimerText(float timeRemaining)
    {
        int timeToShow = Mathf.CeilToInt(timeRemaining);
        timeToShow = Mathf.Clamp(timeToShow, 0, 99);
        timerText.text = $"{timeToShow:00}";
    }
}
