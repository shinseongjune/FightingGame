using UnityEngine;

public class CrouchState : CharacterState
{
    public CrouchState(CharacterFSM f) : base(f) { }
    public override CharacterStateTag? StateTag => CharacterStateTag.Crouch;

    protected override void OnEnter()
    {
        property.isInputEnabled = true;
        phys.SetPose(CharacterStateTag.Crouch);
        Play(property.characterName + "/" + animCfg.GetClipKey(AnimKey.Crouch));
    }

    protected override void OnTick()
    {
        // 1) 스킬 발동 시도
        if (TryStartSkill()) return;

        var d = input != null ? input.LastInput.direction : Direction.Neutral;
        bool keep = d is Direction.Down or Direction.DownBack or Direction.DownForward;
        if (!keep) Transition("Idle");
    }
    protected override void OnExit() { }
}
