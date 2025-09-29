using UnityEngine;

public class IdleState : CharacterState
{
    public IdleState(CharacterFSM f) : base(f) { }
    public override CharacterStateTag? StateTag => CharacterStateTag.Idle;

    protected override void OnEnter()
    {
        property.currentSkill = null;

        property.isInputEnabled = true;
        phys.mode = PhysicsMode.Normal;
        phys.isGravityOn = true;
        phys.SetPose(CharacterStateTag.Idle);
        Play(property.characterName + "/" + animCfg.GetClipKey(AnimKey.Idle));
    }

    protected override void OnTick()
    {
        if (enemy.fsm.Current.StateTag == CharacterStateTag.Idle)
        {
            if (enemy.transform.position.x > tr.position.x)
            {
                property.SetFacing(true);
                enemy.SetFacing(false);
            }
            else
            {
                property.SetFacing(false);
                enemy.SetFacing(true);
            }
        }

        // 1) 스킬 발동 시도
        if (TryStartSkill()) return;

        var d = input != null ? input.LastInput.direction : Direction.Neutral;

        if (d is Direction.Down or Direction.DownBack or Direction.DownForward)
        { Transition("Crouch"); return; }

        if (d == Direction.Forward) { Transition("WalkF"); return; }
        if (d == Direction.Back) { Transition("WalkB"); return; }

        if (d is Direction.Up or Direction.UpForward or Direction.UpBack)
        {
            if (d == Direction.UpForward) Transition("JumpF");
            else if (d == Direction.UpBack) Transition("JumpB");
            else Transition("JumpUp");
        }
    }
    protected override void OnExit() { }
}
