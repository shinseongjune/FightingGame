using UnityEngine;

public class WakeUpState : CharacterState
{
    private CharacterProperty property;
    private AnimationPlayer animator;

    private bool readyToExit = false;

    public WakeUpState(CharacterFSM fsm) : base(fsm)
    {
        this.fsm = fsm;
        this.owner = fsm.gameObject;

        this.property = owner.GetComponent<CharacterProperty>();
        this.animator = owner.GetComponent<AnimationPlayer>();
    }

    public override void OnEnter()
    {
        property.isAttacking = false;
        property.isJumping = false;
        property.isSitting = false;

        animator.Play("WakeUp", OnWakeUpFinished);
        property.EnableDefaultBoxes(CharacterStateTag.Standing);
    }

    private void OnWakeUpFinished()
    {
        readyToExit = true;
    }

    public override void OnUpdate()
    {
        if (readyToExit)
        {
            fsm.TransitionTo(new IdleState(fsm));
        }
    }

    public override void OnExit()
    {

    }
}
