public sealed class DummyInputProvider : IInputProvider
{
    public InputData GetSnapshot()
    {
        return new InputData
        {
            direction = Direction.Neutral,
            attack = AttackKey.None,
            backCharge = 0,
            downCharge = 0,
            isUsed = false,
            tick = 0,
        };
    }
}