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

        // 공중 경직 도중 착지하면 즉시 착지 상태로 전이
        if (physics.grounded)
        {
            fsm.TransitionTo(new LandState(fsm));
            return;
        }

        if (elapsed >= stunDuration)
        {
            // 경직 끝났지만 아직 공중이면 Fall 상태로
            fsm.TransitionTo(new FallState(fsm));
        }
    }

    public override void OnExit()
    {

    }
}
