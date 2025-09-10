using UnityEngine;

public class FallState : CharacterState
{
    public FallState(CharacterFSM f) : base(f) { }
    public override CharacterStateTag? StateTag => CharacterStateTag.Jump_Up; // 점프 포즈 유지

    protected override void OnEnter()
    {
        property.isInputEnabled = true;
        Play(property.characterName + "/" + animCfg.GetClipKey(AnimKey.Fall));
    }

    protected override void OnTick()
    {
        if (TryStartSkill()) return;

        if (phys.isGrounded) Transition("Land");
    }

    protected override void OnExit() { }
}
