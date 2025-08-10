using UnityEngine;

public class BeingThrownState : CharacterState
{
    private CharacterProperty property;
    private AnimationPlayer animator;

    private bool readyToExit = false;

    public BeingThrownState(CharacterFSM fsm) : base(fsm)
    {
        this.property = owner.GetComponent<CharacterProperty>();
        this.animator = owner.GetComponent<AnimationPlayer>();
    }

    public override void OnEnter()
    {
        property.isAttacking = false;
        property.isJumping = false;
        property.isSitting = false;

        property.EnableDefaultBoxes(CharacterStateTag.Down); // 대부분 무방비 상태

        animator.Play("BeingThrown", OnThrowEnd);
    }

    private void OnThrowEnd()
    {
        readyToExit = true;
    }

    public override void OnUpdate()
    {
        if (readyToExit)
        {
            fsm.TransitionTo(new DownState(fsm));
        }
    }

    public override void OnExit()
    {
        // 특별한 처리 없음
    }
}
