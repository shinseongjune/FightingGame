using System.Linq;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class JumpState : CharacterState
{
    private CharacterProperty property;
    private AnimationPlayer animator;
    private PhysicsEntity physics;
    private InputBuffer inputBuffer;

    private float jumpVelocity = 12f; // Ƣ������� y�ӵ�
    private float elapsedTime = 0f;

    public JumpState(CharacterFSM fsm) : base(fsm)
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
        property.isJumping = true;
        property.isSitting = false;
        property.isAttacking = false;

        property.usableSkills = property.jumpSkills.ToList();
        property.EnableDefaultBoxes(CharacterStateTag.Jumping);

        animator.Play("JumpStart");

        // y�ӵ� �ο�
        physics.velocity = new Vector2(physics.velocity.x, jumpVelocity);

        elapsedTime = 0f;
    }

    public override void OnUpdate()
    {
        if (property.currentSkill != null)
        {
            fsm.TransitionTo(new SkillState(fsm));
            return;
        }

        elapsedTime += TickMaster.TICK_INTERVAL;

        // ���� ���� ���� �� �ٷ� FallState ���� ����
        if (physics.velocity.y <= 0f && elapsedTime >= TickMaster.TICK_INTERVAL * 2)
        {
            fsm.TransitionTo(new FallState(fsm));
            return;
        }
    }

    public override void OnExit()
    {

    }
}
