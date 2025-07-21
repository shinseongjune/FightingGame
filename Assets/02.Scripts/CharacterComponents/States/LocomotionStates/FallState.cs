using System.Linq;
using UnityEngine;

public class FallState : CharacterState
{
    private CharacterProperty property;
    private AnimationPlayer animator;
    private PhysicsEntity physics;

    public FallState(CharacterFSM fsm) : base(fsm)
    {
        this.fsm = fsm;
        this.owner = fsm.gameObject;
        this.property = owner.GetComponent<CharacterProperty>();
        this.animator = owner.GetComponent<AnimationPlayer>();
        this.physics = owner.GetComponent<PhysicsEntity>();
    }

    public override void OnEnter()
    {
        property.isJumping = true;
        property.isSitting = false;
        property.isAttacking = false;

        property.usableSkills = property.jumpSkills.ToList();
        property.EnableDefaultBoxes(CharacterStateTag.Jumping);

        animator.Play("Fall");
    }

    public override void OnUpdate()
    {
        if (property.currentSkill != null)
        {
            fsm.TransitionTo(new SkillState(fsm));
            return;
        }

        if (physics.grounded)
        {
            fsm.TransitionTo(new LandState(fsm));
        }
    }

    public override void OnExit()
    {

    }
}
