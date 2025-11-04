using UnityEngine;

/// <summary>
/// 실제 프로젝트의 InputData 구조에 맞춰, 방향/공격키/차지/틱을 간편하게 조립하는 빌더.
/// - 방향은 CharacterProperty.isFacingRight를 고려해 Forward/Back을 올바르게 설정
/// - 공격키는 AttackKey 비트플래그를 누적
/// - 차지는 필요시 세팅(기본 0)
/// </summary>
public sealed class InputDataBuilder
{
    private readonly CharacterProperty character; // facing 판정용 (null이면 기본 오른쪽 바라본다고 가정)
    private Direction dir = Direction.Neutral;
    private AttackKey atk = AttackKey.None;
    private int backCharge = 0;
    private int downCharge = 0;

    public InputDataBuilder(CharacterProperty owner)
    {
        character = owner;
    }

    // ===== 방향 도우미 =====
    public InputDataBuilder Neutral() { dir = Direction.Neutral; return this; }

    public InputDataBuilder Forward()
    {
        bool right = character == null || character.isFacingRight;
        dir = right ? Direction.Forward : Direction.Back;
        return this;
    }

    public InputDataBuilder Back()
    {
        bool right = character == null || character.isFacingRight;
        dir = right ? Direction.Back : Direction.Forward;
        return this;
    }

    public InputDataBuilder Up()
    {
        dir = Direction.Up;
        return this;
    }

    public InputDataBuilder Down()
    {
        dir = Direction.Down;
        return this;
    }

    public InputDataBuilder UpForward()
    {
        bool right = character == null || character.isFacingRight;
        dir = right ? Direction.UpForward : Direction.UpBack;
        return this;
    }

    public InputDataBuilder UpBack()
    {
        bool right = character == null || character.isFacingRight;
        dir = right ? Direction.UpBack : Direction.UpForward;
        return this;
    }

    public InputDataBuilder DownForward()
    {
        bool right = character == null || character.isFacingRight;
        dir = right ? Direction.DownForward : Direction.DownBack;
        return this;
    }

    public InputDataBuilder DownBack()
    {
        bool right = character == null || character.isFacingRight;
        dir = right ? Direction.DownBack : Direction.DownForward;
        return this;
    }

    /// <summary>
    /// '점프'를 하고 싶을 때 보통 위/위앞/위뒤 중 하나를 써요.
    /// 기본은 위앞 점프로 가정 (전진 점프).
    /// </summary>
    public InputDataBuilder Jump(bool forward = true)
    {
        if (forward) return UpForward();
        else return UpBack();
    }

    // ===== 공격키 =====
    public InputDataBuilder Press(AttackKey key)
    {
        atk |= key;
        return this;
    }

    public InputDataBuilder SetAttack(AttackKey key)
    {
        atk = key;
        return this;
    }

    // ===== 차지(선택) =====
    public InputDataBuilder SetCharges(int back, int down)
    {
        backCharge = Mathf.Max(0, back);
        downCharge = Mathf.Max(0, down);
        return this;
    }

    // ===== 최종 빌드 =====
    public InputData Build(long tick)
    {
        return new InputData
        {
            direction = dir,
            attack = atk,
            backCharge = backCharge,
            downCharge = downCharge,
            isUsed = false,
            tick = tick,
        };
    }
}
