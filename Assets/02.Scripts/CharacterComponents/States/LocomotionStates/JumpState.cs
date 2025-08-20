using UnityEngine;

public abstract class BaseJumpState : CharacterState
{
    protected float jumpSpeed = 7.5f;
    protected float horizSpeed = 2.8f;

    protected BaseJumpState(CharacterFSM f) : base(f) { }

    protected override void OnEnter()
    {
        property.isInputEnabled = true;
        phys.isGrounded = false;
        phys.mode = PhysicsMode.Normal;
        phys.isGravityOn = true;
        phys.SetPose(CharacterStateTag.Jump_Up);
        Play(animCfg.GetClipKey(AnimKey.JumpUp));
        phys.Velocity = new Vector2(Horizontal(), jumpSpeed);
    }

    protected override void OnTick()
    {
        if (TryStartSkill()) return;

        if (phys.Velocity.y <= 0f) Transition("Fall");
    }

    protected virtual float Horizontal() => 0f;
    protected override void OnExit() { }
}

public class JumpUpState : BaseJumpState
{
    public JumpUpState(CharacterFSM f) : base(f) { }
    public override CharacterStateTag? StateTag => CharacterStateTag.Jump_Up;
}

public class JumpForwardState : BaseJumpState
{
    public JumpForwardState(CharacterFSM f) : base(f) { }
    public override CharacterStateTag? StateTag => CharacterStateTag.Jump_Forward;
    protected override float Horizontal() => +horizSpeed;
}

public class JumpBackwardState : BaseJumpState
{
    public JumpBackwardState(CharacterFSM f) : base(f) { }
    public override CharacterStateTag? StateTag => CharacterStateTag.Jump_Backward;
    protected override float Horizontal() => -horizSpeed;
}
