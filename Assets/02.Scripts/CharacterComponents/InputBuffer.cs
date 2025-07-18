using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterProperty))]
public class InputBuffer : MonoBehaviour, ITicker
{
    private CharacterProperty character;

    public int maxBufferSize = 30;
    public Queue<InputData> inputQueue { get; private set; } = new();

    private InputSystem_Actions inputActions;

    private InputData previousInput;
    private int backHold = 0;
    private int downHold = 0;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        inputActions.Enable(); // 전체 맵 Enable
        character = GetComponent<CharacterProperty>();
    }

    public void Tick()
    {
        Vector2 move = inputActions.Player.Move.ReadValue<Vector2>();
        Direction dir = ToDirection(move);
        AttackKey attack = ReadAttackKey();

        // 방향별 차지 지속 시간 계산
        if (ContainsBack(dir) && ContainsBack(previousInput.direction)) backHold++;
        else backHold = 0;

        if (ContainsDown(dir) && ContainsDown(previousInput.direction)) downHold++;
        else downHold = 0;

        InputData input = new()
        {
            direction = dir,
            attack = attack,
            backCharge = backHold,
            downCharge = downHold
        };

        previousInput = input;

        inputQueue.Enqueue(input);
        while (inputQueue.Count > maxBufferSize)
            inputQueue.Dequeue();
    }

    private bool ContainsBack(Direction dir) =>
    dir == Direction.Back || dir == Direction.UpBack || dir == Direction.DownBack;

    private bool ContainsDown(Direction dir) =>
        dir == Direction.Down || dir == Direction.DownBack || dir == Direction.DownForward;

    private Direction ToDirection(Vector2 input)
    {
        input = input.normalized;

        // 캐릭터 방향 고려
        bool isFacingRight = character == null || character.isFacingRight;

        // 좌우 반전
        float horizontal = isFacingRight ? input.x : -input.x;

        // 우선순위 처리
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
        if (inputActions.Player.LP.IsPressed()) key |= AttackKey.LP;
        if (inputActions.Player.MP.IsPressed()) key |= AttackKey.MP;
        if (inputActions.Player.HP.IsPressed()) key |= AttackKey.HP;
        if (inputActions.Player.LK.IsPressed()) key |= AttackKey.LK;
        if (inputActions.Player.MK.IsPressed()) key |= AttackKey.MK;
        if (inputActions.Player.HK.IsPressed()) key |= AttackKey.HK;
        return key;
    }
}
