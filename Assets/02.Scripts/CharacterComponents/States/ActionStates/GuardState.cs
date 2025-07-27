using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class GuardState : CharacterState
{
    private CharacterProperty property;
    private AnimationPlayer animator;

    public GuardState(CharacterFSM fsm) : base(fsm)
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

        property.usableSkills = property.idleSkills.ToList();
        property.EnableDefaultBoxes(CharacterStateTag.Standing);

        animator.Play("Idle");
    }

    public override void OnUpdate()
    {
        if (property.currentSkill != null)
        {
            fsm.TransitionTo(new SkillState(fsm));
            return;
        }

        InputBuffer input = owner.GetComponent<InputBuffer>();
        if (input == null) return;

        InputData latest = input.inputQueue.Count > 0 ? input.inputQueue.Peek() : default;

        if (latest.direction == Direction.Down || latest.direction == Direction.DownBack || latest.direction == Direction.DownForward)
        {
            fsm.TransitionTo(new CrouchState(fsm));
        }
        else if (latest.direction == Direction.Up || latest.direction == Direction.UpForward || latest.direction == Direction.UpBack)
        {
            fsm.TransitionTo(new JumpState(fsm));
        }
        else if (latest.direction == Direction.Forward || latest.direction == Direction.Back)
        {
            fsm.TransitionTo(new WalkState(fsm));
        }
    }

    public override void OnExit()
    {

    }
}
