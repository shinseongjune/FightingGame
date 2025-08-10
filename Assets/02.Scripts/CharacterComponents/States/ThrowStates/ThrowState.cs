using UnityEngine;

public class ThrowState : CharacterState
{
    private CharacterProperty property;
    private AnimationPlayer animator;

    private bool readyToExit = false;

    public ThrowState(CharacterFSM fsm) : base(fsm)
    {
        this.property = owner.GetComponent<CharacterProperty>();
        this.animator = owner.GetComponent<AnimationPlayer>();
    }

    public override void OnEnter()
    {
        property.isAttacking = true;
        property.EnableDefaultBoxes(CharacterStateTag.Standing);

        // 추후 스킬 기반 연출도 가능하지만 지금은 고정
        animator.Play("Throw", OnThrowAnimationFinished);
    }

    private void OnThrowAnimationFinished()
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
        property.isAttacking = false;
    }
}
