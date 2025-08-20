using UnityEngine;

public class WalkForwardState : CharacterState
{
    public float moveSpeed = 3.5f;

    public WalkForwardState(CharacterFSM f) : base(f) { }
    public override CharacterStateTag? StateTag => CharacterStateTag.Walk_Forward;

    protected override void OnEnter()
    {
        property.isInputEnabled = true;
        phys.SetPose(CharacterStateTag.Walk_Forward);
        Play(animCfg.GetClipKey(AnimKey.WalkF));
    }

    protected override void OnTick()
    {
        // 1) 스킬 발동 시도
        if (TryStartSkill()) return;

        var d = input != null ? input.LastInput.direction : Direction.Neutral;
        if (d != Direction.Forward) { Transition("Idle"); return; }

        int sx = property.isFacingRight ? 1 : -1; // 방향 보정
        phys.Position += new Vector2(sx * moveSpeed * TickMaster.TICK_INTERVAL, 0f);
    }
    protected override void OnExit() { }
}

public class WalkBackwardState : CharacterState
{
    public float moveSpeed = 3.0f;

    public WalkBackwardState(CharacterFSM f) : base(f) { }
    public override CharacterStateTag? StateTag => CharacterStateTag.Walk_Backward;

    protected override void OnEnter()
    {
        property.isInputEnabled = true;
        phys.SetPose(CharacterStateTag.Walk_Backward);
        Play(animCfg.GetClipKey(AnimKey.WalkB));
    }

    protected override void OnTick()
    {
        // 1) 스킬 발동 시도
        if (TryStartSkill()) return;

        var d = input != null ? input.LastInput.direction : Direction.Neutral;
        if (d != Direction.Back) { Transition("Idle"); return; }

        int sx = property.isFacingRight ? 1 : -1; // 방향 보정
        phys.Position += new Vector2(sx * -moveSpeed * TickMaster.TICK_INTERVAL, 0f);
    }
    protected override void OnExit() { }
}
