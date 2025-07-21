using UnityEngine;

public class KnockbackState : CharacterState
{
    private CharacterProperty property;
    private AnimationPlayer animator;
    private PhysicsEntity physics;

    public KnockbackState(CharacterFSM fsm) : base(fsm)
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

        property.EnableDefaultBoxes(CharacterStateTag.Jumping);
        animator.Play("Knockback");

        var hit = property.lastHitInfo;

        // ������ �˹� ���� (����, ���� Ƣ���)
        float xForce = hit.fromFront ? -5f : 5f;
        float yForce = 8f;

        physics.velocity = new Vector2(xForce, yForce);
    }

    public override void OnUpdate()
    {
        if (physics.grounded)
        {
            fsm.TransitionTo(new DownState(fsm));
        }
    }

    public override void OnExit()
    {
        // Ư���� ó�� ����
    }
}
