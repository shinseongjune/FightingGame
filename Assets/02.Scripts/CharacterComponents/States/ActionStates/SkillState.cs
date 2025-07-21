using UnityEngine;

public class SkillState : CharacterState
{
    private CharacterProperty property;
    private AnimationPlayer animator;
    private BoxPresetApplier boxApplier;

    private Skill currentSkill;

    public SkillState(CharacterFSM fsm) : base(fsm)
    {
        this.fsm = fsm;
        this.owner = fsm.gameObject;
        this.property = owner.GetComponent<CharacterProperty>();
        this.animator = owner.GetComponent<AnimationPlayer>();
        this.boxApplier = owner.GetComponent<BoxPresetApplier>();
    }

    public override void OnEnter()
    {
        currentSkill = property.currentSkill;

        if (currentSkill == null)
        {
            Debug.LogError("[SkillState] currentSkill is null.");
            fsm.TransitionTo(new IdleState(fsm));
            return;
        }

        property.isAttacking = true;
        boxApplier.currentSkill = currentSkill;

        animator.Play(currentSkill.animationClipName, OnSkillFinished);
    }

    private void OnSkillFinished()
    {
        property.isAttacking = false;
        property.currentSkill = null;
        boxApplier.currentSkill = null;

        // 상태 복귀 (공중이면 Fall, 앉았으면 Crouch, 기본은 Idle)
        if (property.isJumping)
            fsm.TransitionTo(new FallState(fsm));
        else if (property.isSitting)
            fsm.TransitionTo(new CrouchState(fsm));
        else
            fsm.TransitionTo(new IdleState(fsm));
    }

    public override void OnUpdate()
    {
        // 스킬 중에는 입력 무시
    }

    public override void OnExit()
    {
        property.isAttacking = false;
        property.currentSkill = null;
        boxApplier.currentSkill = null;
    }
}
