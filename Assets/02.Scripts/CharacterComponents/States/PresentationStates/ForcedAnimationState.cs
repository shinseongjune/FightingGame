using UnityEngine;

public class ForcedAnimationState : CharacterState
{
    private CharacterProperty property;
    private AnimationPlayer animator;

    private readonly string clipName;
    private readonly CharacterState nextState;

    private bool finished = false;

    public ForcedAnimationState(CharacterFSM fsm, string clipName, CharacterState nextState = null) : base(fsm)
    {
        this.clipName = clipName;
        this.nextState = nextState ?? new IdleState(fsm); // �⺻ ���ʹ� Idle
        this.property = owner.GetComponent<CharacterProperty>();
        this.animator = owner.GetComponent<AnimationPlayer>();
    }

    public override void OnEnter()
    {
        property.isAttacking = false;
        property.isJumping = false;
        property.isSitting = false;
        property.isSpecialPosing = true;

        property.EnableDefaultBoxes(CharacterStateTag.Standing); // �Ǵ� �ʿ� ���� ���� ����

        animator.Play(clipName, OnComplete);
    }

    private void OnComplete()
    {
        finished = true;
    }

    public override void OnUpdate()
    {
        if (finished)
        {
            fsm.TransitionTo(nextState);
        }
    }

    public override void OnExit()
    {
        property.isSpecialPosing = false;
    }
}
