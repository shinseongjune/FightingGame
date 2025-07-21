using System.Linq;
using UnityEngine;

public class CrouchState : CharacterState
{
    private CharacterProperty property;
    private AnimationPlayer animator;
    private InputBuffer inputBuffer;

    public CrouchState(CharacterFSM fsm) : base(fsm)
    {
        this.fsm = fsm;
        this.owner = fsm.gameObject;
        this.property = owner.GetComponent<CharacterProperty>();
        this.animator = owner.GetComponent<AnimationPlayer>();
        this.inputBuffer = owner.GetComponent<InputBuffer>();
    }

    public override void OnEnter()
    {
        property.isSitting = true;
        property.isJumping = false;
        property.isAttacking = false;

        property.usableSkills = property.crouchSkills.ToList();
        property.EnableDefaultBoxes(CharacterStateTag.Crouching);

        animator.Play("Crouch");
    }

    public override void OnUpdate()
    {
        if (inputBuffer.inputQueue.Count == 0)
            return;

        if (property.currentSkill != null)
        {
            fsm.TransitionTo(new SkillState(fsm));
            return;
        }

        InputData latest = inputBuffer.inputQueue.Peek();

        bool up = latest.direction is Direction.Up or Direction.UpForward or Direction.UpBack;
        bool down = latest.direction is Direction.Down or Direction.DownForward or Direction.DownBack;

        if (up)
        {
            fsm.TransitionTo(new JumpState(fsm));
            return;
        }

        if (!down)
        {
            fsm.TransitionTo(new IdleState(fsm));
            return;
        }
    }

    public override void OnExit()
    {

    }
}
