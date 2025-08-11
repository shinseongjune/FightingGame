using UnityEngine;

public class LandState : CharacterState
{
    const int LandFreeze = 6; // 착지 경직 프레임

    public LandState(CharacterFSM f) : base(f) { }
    public override CharacterStateTag? StateTag => CharacterStateTag.Idle;

    protected override void OnEnter()
    {
        phys.SetPose(CharacterStateTag.Idle);
        Play(animCfg.GetClipKey(AnimKey.Land));
    }

    protected override void OnTick()
    {
        if (TryStartSkill()) return;

        if (ElapsedFrames >= LandFreeze) Transition("Idle");
    }

    protected override void OnExit() { }
}
