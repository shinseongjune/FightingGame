using UnityEngine;

public class BlockStunState : CharacterState
{
    private CharacterProperty property;
    private AnimationPlayer animator;

    private float stunDuration;
    private float elapsed;

    public BlockStunState(CharacterFSM fsm) : base(fsm)
    {
        this.fsm = fsm;
        this.owner = fsm.gameObject;

        this.property = owner.GetComponent<CharacterProperty>();
        this.animator = owner.GetComponent<AnimationPlayer>();
    }

    public override void OnEnter()
    {
        var hit = property.lastHitInfo;
        stunDuration = hit.blockStun * TickMaster.TICK_INTERVAL;
        elapsed = 0f;

        property.isAttacking = false;

        if (property.isSitting)
            property.EnableDefaultBoxes(CharacterStateTag.Crouching);
        else
            property.EnableDefaultBoxes(CharacterStateTag.Standing);

        animator.Play("BlockStun");
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
        // 특별한 처리 없음
    }
}
