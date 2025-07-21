using UnityEngine;

public class AirStunState : CharacterState
{
    private CharacterProperty property;
    private AnimationPlayer animator;
    private PhysicsEntity physics;

    private float stunDuration;
    private float elapsed;

    public AirStunState(CharacterFSM fsm) : base(fsm)
    {
        this.fsm = fsm;
        this.owner = fsm.gameObject;

        this.property = owner.GetComponent<CharacterProperty>();
        this.animator = owner.GetComponent<AnimationPlayer>();
        this.physics = owner.GetComponent<PhysicsEntity>();
    }

    public override void OnEnter()
    {
        property.isAttacking = false;
        property.isJumping = true;

        var hitInfo = property.lastHitInfo;
        stunDuration = hitInfo.hitStun * TickMaster.TICK_INTERVAL;
        elapsed = 0f;

        property.EnableDefaultBoxes(CharacterStateTag.Jumping);

        animator.Play("AirStun");
    }

    public override void OnUpdate()
    {
        elapsed += TickMaster.TICK_INTERVAL;

        // ���� ���� ���� �����ϸ� ��� ���� ���·� ����
        if (physics.grounded)
        {
            fsm.TransitionTo(new LandState(fsm));
            return;
        }

        if (elapsed >= stunDuration)
        {
            // ���� �������� ���� �����̸� Fall ���·�
            fsm.TransitionTo(new FallState(fsm));
        }
    }

    public override void OnExit()
    {

    }
}
