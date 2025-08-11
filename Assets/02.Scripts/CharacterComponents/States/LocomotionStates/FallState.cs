using UnityEngine;

public class FallState : CharacterState
{
    public FallState(CharacterFSM f) : base(f) { }
    public override CharacterStateTag? StateTag => CharacterStateTag.Jump_Up; // ���� ���� ����

    protected override void OnEnter()
    {
        Play(animCfg.GetClipKey(AnimKey.Fall));
    }

    protected override void OnTick()
    {
        if (TryStartSkill()) return;

        if (phys.isGrounded) Transition("Land");
    }

    protected override void OnExit() { }
}
