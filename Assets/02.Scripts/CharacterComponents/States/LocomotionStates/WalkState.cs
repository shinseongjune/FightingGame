using System.Linq;
using UnityEngine;

public class WalkState : CharacterState
{
    private CharacterProperty property;
    private AnimationPlayer animator;
    private PhysicsEntity physics;
    private InputBuffer inputBuffer;

    public WalkState(CharacterFSM fsm) : base(fsm)
    {
        this.fsm = fsm;
        this.owner = fsm.gameObject;
        this.property = owner.GetComponent<CharacterProperty>();
        this.animator = owner.GetComponent<AnimationPlayer>();
        this.physics = owner.GetComponent<PhysicsEntity>();
        this.inputBuffer = owner.GetComponent<InputBuffer>();
    }

    public override void OnEnter()
    {
        property.isJumping = false;
        property.isSitting = false;
        property.isAttacking = false;

        property.usableSkills = property.idleSkills.ToList();
        property.EnableDefaultBoxes(CharacterStateTag.Standing);

        animator.Play("Walk");
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
        bool forward = latest.direction == Direction.Forward;
        bool back = latest.direction == Direction.Back;

        // 상태 전이 우선순위
        if (up)
        {
            fsm.TransitionTo(new JumpState(fsm));
            return;
        }
        if (down)
        {
            fsm.TransitionTo(new CrouchState(fsm));
            return;
        }
        if (!forward && !back)
        {
            fsm.TransitionTo(new IdleState(fsm));
            return;
        }

        // 이동 처리 (간단히 x축 속도 설정)
        float moveSpeed = 5f;
        physics.velocity = new Vector2(
            forward ? moveSpeed : (back ? -moveSpeed : 0f),
            physics.velocity.y
        );
    }

    public override void OnExit()
    {
        // 정지 처리
        physics.velocity = new Vector2(0, physics.velocity.y);
    }
}
