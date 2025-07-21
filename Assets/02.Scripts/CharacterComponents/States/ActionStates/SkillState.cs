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

        // ���� ���� (�����̸� Fall, �ɾ����� Crouch, �⺻�� Idle)
        if (property.isJumping)
            fsm.TransitionTo(new FallState(fsm));
        else if (property.isSitting)
            fsm.TransitionTo(new CrouchState(fsm));
        else
            fsm.TransitionTo(new IdleState(fsm));
    }

    public override void OnUpdate()
    {
        // ��ų �߿��� �Է� ����
    }

    public override void OnExit()
    {
        property.isAttacking = false;
        property.currentSkill = null;
        boxApplier.currentSkill = null;
    }
}
