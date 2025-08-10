using UnityEngine;

public class HitStun_GroundState : CharacterState
{
    private CharacterProperty property;
    private AnimationPlayer animator;

    private float stunDuration;
    private float elapsed;

    public HitStun_GroundState(CharacterFSM fsm) : base(fsm)
    {
        this.fsm = fsm;
        this.owner = fsm.gameObject;
        this.property = owner.GetComponent<CharacterProperty>();
        this.animator = owner.GetComponent<AnimationPlayer>();
    }

    public override void OnEnter()
    {
        property.isAttacking = false;

        var hitInfo = property.lastHitInfo;
        stunDuration = hitInfo.hitStun * TickMaster.TICK_INTERVAL;
        elapsed = 0f;

        // 기본적으로 스탠딩 박스
        property.EnableDefaultBoxes(CharacterStateTag.Standing);

        animator.Play("HitStun");
    }

    public override void OnUpdate()
    {
        elapsed += TickMaster.TICK_INTERVAL;

        if (elapsed >= stunDuration)
        {
            if (property.isSitting)
                fsm.TransitionTo(new CrouchState(fsm));
            else
                fsm.TransitionTo(new IdleState(fsm));
        }
    }

    public override void OnExit()
    {

    }
}
