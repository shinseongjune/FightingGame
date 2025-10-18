using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterProperty))]
public class InputBuffer : MonoBehaviour
{
    private CharacterProperty character;

    public int maxBufferSize = 30;
    public Queue<InputData> inputQueue { get; private set; } = new();

    private InputSystem_Actions inputActions;

    private InputData previousInput;
    private int backHold = 0;
    private int downHold = 0;

    public InputData LastInput;

    public bool captureFromDevice = true;          // 하드웨어 입력 캡처 on/off
    public bool respectCharacterInputLock = true;  // isInputEnabled=false면 강제로 중립 노출
    public bool enqueueNeutralWhenLocked = true;   // 잠금 시에도 중립을 큐에 넣어 타임라인 전진

    public bool useArbiterSource = false;
    public InputArbiter arbiter;

    private void Awake()
    {
        inputActions = GameManager.Instance.actions;
        inputActions.Enable(); // 전체 맵 Enable
        character = GetComponent<CharacterProperty>();
    }

    public void Tick()
    {
        // 0) 입력 캡처 off면: 외부 주입만 사용(아무 것도 하지 않음)
        if (!captureFromDevice && !useArbiterSource)
            return;

        // 1) 원시 스냅샷 확보
        InputData snap;
        if (useArbiterSource && arbiter != null)
        {
            // Arbiter를 통해 스냅샷 획득 (direction/attack만 중요)
            snap = arbiter.Resolve();
        }
        else
        {
            // 기존 로컬 입력 경로 유지
            Vector2 move = inputActions.Player.Move.ReadValue<Vector2>();
            Direction dir = ToDirection(move);
            AttackKey attack = ReadAttackKey();

            snap = new InputData
            {
                direction = dir,
                attack = attack,
                backCharge = 0,
                downCharge = 0
            };
        }

        // 2) 캐릭터 입력 락 처리
        bool locked = respectCharacterInputLock && character != null && !character.isInputEnabled;
        if (locked)
        {
            snap.direction = Direction.Neutral;
            snap.attack = AttackKey.None;
            backHold = 0; downHold = 0;

            // ★ 틱 스탬프
            snap.tick = (TickMaster.Instance != null) ? TickMaster.Instance.CurrentTick : 0;

            if (!enqueueNeutralWhenLocked)
            {
                LastInput = snap;
                previousInput = snap;
                return;
            }
        }
        else
        {
            // 3) hold 계산 (기존 로직 유지)
            if (ContainsBack(snap.direction) && ContainsBack(previousInput.direction)) backHold++;
            else backHold = 0;

            if (ContainsDown(snap.direction) && ContainsDown(previousInput.direction)) downHold++;
            else downHold = 0;
        }

        // 4) tick 스탬프 + charge 반영
        snap.tick = (TickMaster.Instance != null) ? TickMaster.Instance.CurrentTick : 0;
        snap.backCharge = backHold;
        snap.downCharge = downHold;

        previousInput = snap;
        LastInput = snap;

        inputQueue.Enqueue(snap);
        while (inputQueue.Count > maxBufferSize) inputQueue.Dequeue();
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

    // 외부 주입(네트워크/AI용)
    public void PushExternal(InputData input, bool enqueue = true)
    {
        if (input.tick == 0 && TickMaster.Instance != null)
            input.tick = TickMaster.Instance.CurrentTick;

        LastInput = input;
        previousInput = input;
        if (enqueue)
        {
            inputQueue.Enqueue(input);
            while (inputQueue.Count > maxBufferSize) inputQueue.Dequeue();
        }
    }

    public void ClearBuffer()
    {
        inputQueue.Clear();
        previousInput = default;
        LastInput = default;
        backHold = 0;
        downHold = 0;
    }
}
