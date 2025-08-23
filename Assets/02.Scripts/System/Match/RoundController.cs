using UnityEngine;
using System;

public class RoundController : MonoBehaviour, ITicker
{
    public CharacterProperty p1, p2;
    public int timerSeconds = 99;
    public bool roundActive { get; private set; }
    float remain;
    public event Action<bool> OnRoundEnd; // true=p1½Â

    void OnEnable() { TickMaster.Instance?.Register(this); }
    void OnDisable() { TickMaster.Instance?.Unregister(this); }

    public void StartRound(int seconds)
    {
        timerSeconds = seconds;
        remain = timerSeconds;
        roundActive = true;
    }
    public void StopRound() => roundActive = false;

    public void Tick()
    {
        if (!roundActive) return;

        remain -= TickMaster.TICK_INTERVAL;
        if (remain <= 0f) { roundActive = false; OnRoundEnd?.Invoke(p1.hp >= p2.hp); return; }
        if (p1.hp <= 0f) { roundActive = false; OnRoundEnd?.Invoke(false); return; }
        if (p2.hp <= 0f) { roundActive = false; OnRoundEnd?.Invoke(true); return; }
    }

    public int RemainSeconds => Mathf.CeilToInt(Mathf.Max(remain, 0f));
}
