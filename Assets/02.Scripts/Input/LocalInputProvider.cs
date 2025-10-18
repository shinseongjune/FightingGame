using UnityEngine;

public sealed class LocalInputProvider : IInputProvider
{
    private readonly InputSystem_Actions actions;
    private readonly CharacterProperty character;

    public LocalInputProvider(InputSystem_Actions actions, CharacterProperty owner)
    {
        this.actions = actions;
        this.character = owner;
    }

    public InputData GetSnapshot()
    {
        var move = actions.Player.Move.ReadValue<Vector2>();
        var dir = ToDirection(move);
        var atk = ReadAttackKey();

        var data = new InputData
        {
            tick = TickMaster.Instance != null ? TickMaster.Instance.CurrentTick : 0,
            direction = dir,
            attack = atk,
            backCharge = 0,
            downCharge = 0,
            isUsed = false
        };
        return data;
    }

    private Direction ToDirection(Vector2 input)
    {
        input = input.normalized;
        bool isFacingRight = character == null || character.isFacingRight;
        float horizontal = isFacingRight ? input.x : -input.x;

        bool up = input.y > 0.5f;
        bool down = input.y < -0.5f;
        bool back = horizontal < -0.5f;
        bool forward = horizontal > 0.5f;

        if (up && forward) return Direction.UpForward;
        if (up && back) return Direction.UpBack;
        if (down && forward) return Direction.DownForward;
        if (down && back) return Direction.DownBack;
        if (up) return Direction.Up;
        if (down) return Direction.Down;
        if (forward) return Direction.Forward;
        if (back) return Direction.Back;
        return Direction.Neutral;
    }

    private AttackKey ReadAttackKey()
    {
        AttackKey key = AttackKey.None;
        if (actions.Player.LP.IsPressed()) key |= AttackKey.LP;
        if (actions.Player.MP.IsPressed()) key |= AttackKey.MP;
        if (actions.Player.HP.IsPressed()) key |= AttackKey.HP;
        if (actions.Player.LK.IsPressed()) key |= AttackKey.LK;
        if (actions.Player.MK.IsPressed()) key |= AttackKey.MK;
        if (actions.Player.HK.IsPressed()) key |= AttackKey.HK;
        return key;
    }
}