using UnityEngine;

public class DownState : CharacterState
{
    private CharacterProperty property;
    private AnimationPlayer animator;

    private float downDuration = 1.5f; // �ٿ� ���� �ð� (��)
    private float elapsed;

    public DownState(CharacterFSM fsm) : base(fsm)
    {
        this.fsm = fsm;
        this.owner = fsm.gameObject;

        this.property = owner.GetComponent<CharacterProperty>();
        this.animator = owner.GetComponent<AnimationPlayer>();
    }

    public override void OnEnter()
    {
        property.isAttacking = false;
        property.isJumping = false;
        property.isSitting = false;

        elapsed = 0f;
        animator.Play("Down");

        // �⺻������ �ڽ��� ���ų�, �ٿ� ���� ����� ���·� ����
        property.EnableDefaultBoxes(CharacterStateTag.Down);
    }

    public override void OnUpdate()
    {
        elapsed += TickMaster.TICK_INTERVAL;

        if (elapsed >= downDuration)
        {
            fsm.TransitionTo(new WakeUpState(fsm));
        }
    }

    public override void OnExit()
    {

    }
}
