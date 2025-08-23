using UnityEngine;

[System.Serializable]
public struct MatchRulesData
{
    public int roundTimerSeconds;
    public int winTarget;
    public int preRoundFreezeTicks;
    public int koFreezeTicks;

    public Vector2 p1Spawn;
    public Vector2 p2Spawn;
    public bool p1FacingRight;
    public bool p2FacingRight;

    public static MatchRulesData Default => new()
    {
        roundTimerSeconds = 99,
        winTarget = 2,
        preRoundFreezeTicks = 45,
        koFreezeTicks = 60,
        p1Spawn = new(-3f, 0f),
        p2Spawn = new(+3f, 0f),
        p1FacingRight = true,
        p2FacingRight = false
    };
}