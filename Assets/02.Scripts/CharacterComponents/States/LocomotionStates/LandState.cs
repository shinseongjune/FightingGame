using UnityEngine;

public class LandState : CharacterState
{
    private CharacterProperty property;
    private AnimationPlayer animator;

    private bool transitionQueued = false;

    public LandState(CharacterFSM fsm) : base(fsm)
    {
        this.fsm = fsm;
        this.owner = fsm.gameObject;
        this.property = owner.GetComponent<CharacterProperty>();
        this.animator = owner.GetComponent<AnimationPlayer>();
    }

    public override void OnEnter()
    {
        property.isJumping = false;
        property.isSitting = false;
        property.isAttacking = false;
        property.EnableDefaultBoxes(CharacterStateTag.Standing);

        transitionQueued = false;
        animator.Play("Land", OnLandFinished); // 착지 애니메이션 후 Idle로 전이
    }

    private void OnLandFinished()
    {
        transitionQueued = true;
    }

    public override void OnUpdate()
    {
        if (property.currentSkill != null)
        {
            fsm.TransitionTo(new SkillState(fsm));
            return;
        }

        if (transitionQueued)
        {
            fsm.TransitionTo(new IdleState(fsm));
        }
    }

    public override void OnExit()
    {
        // 특별한 처리 없음
    }
}
