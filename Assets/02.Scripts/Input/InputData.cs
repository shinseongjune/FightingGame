using System;
using UnityEngine;

public enum Direction
{
    Neutral,
    Up,
    Down,
    Forward,
    Back,
    UpForward,
    UpBack,
    DownForward,
    DownBack,
}

[Flags]
public enum AttackKey
{
    None = 0,
    LP = 1 << 0,
    MP = 1 << 1,
    HP = 1 << 2,
    LK = 1 << 3,
    MK = 1 << 4,
    HK = 1 << 5,
}

[Serializable]
public struct InputData
{
    public Direction direction;
    public AttackKey attack;
    public int backCharge;
    public int downCharge;
}