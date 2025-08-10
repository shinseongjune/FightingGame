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

        property.EnableDefaultBoxes(CharacterStateTag.Down); // ��κ� ����� ����

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
        // Ư���� ó�� ����
    }
}
