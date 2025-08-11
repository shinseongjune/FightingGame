// ThrowStates.cs
using UnityEngine;

/// <summary> ������(������) �� ��� ���� </summary>
public class ThrowState : CharacterState
{
    PhysicsEntity target;      // ���� ���
    Vector2 holdOffset = new Vector2(0.6f, 0.9f); // ������ ���� ����� ��ġ

    public void SetTarget(PhysicsEntity t) => target = t;

    public ThrowState(CharacterFSM f) : base(f) { }
    public override CharacterStateTag? StateTag => CharacterStateTag.Throw;

    protected override void OnEnter()
    {
        property.isInputEnabled = false;
        phys.mode = PhysicsMode.Kinematic;   // ���� ���� ��Ʈ ����
        phys.isGravityOn = false;

        Play(animCfg.GetClipKey(AnimKey.ThrowStart), OnCatchMoment);
    }

    protected override void OnTick() { }

    // ThrowStart ������ Ÿ�ֿ̹� ���� ����⡯
    void OnCatchMoment()
    {
        if (target != null)
        {
            target.mode = PhysicsMode.Carried;
            target.followTarget = phys;
            target.followOffset = holdOffset;
        }

        // �̾ ������ ���
        Play(animCfg.GetClipKey(AnimKey.ThrowEnd), OnThrowRelease);
    }

    void OnThrowRelease()
    {
        if (target != null)
        {
            // ����� ���� + ������ �ӵ� �ο�
            target.mode = PhysicsMode.Normal;
            target.followTarget = null;

            // ������ ������ ��(�ʿ�� Skill_SO�� �Ķ����ȭ)
            Vector2 launch = new Vector2(property.isFacingRight ? 6f : -6f, 8f);
            target.Velocity = launch;

            // �´� ���� ���� �ǰ�/�ٿ�����
            var targetFSM = target.GetComponent<CharacterFSM>();
            if (targetFSM != null)
            {
                // ��Ȳ�� �°� �ϵ�/�Ϲ� �ٿ� �б�
                targetFSM.TransitionTo("HardKnockdown");
            }
        }

        // �����ڴ� �ĵ� ó��
        ReturnToNeutralPose();
    }

    protected override void OnExit()
    {
        property.isInputEnabled = true;
        phys.mode = PhysicsMode.Normal;
        phys.isGravityOn = true;
    }
}

/// <summary> �ǰ���(���� ��) ���� </summary>
public class BeingThrownState : CharacterState
{
    public BeingThrownState(CharacterFSM f) : base(f) { }
    public override CharacterStateTag? StateTag => CharacterStateTag.BeingThrown;

    protected override void OnEnter()
    {
        property.isInputEnabled = false;
        phys.mode = PhysicsMode.Carried; // ThrowState���� followTarget ����
        phys.isGravityOn = false;

        Play(animCfg.GetClipKey(AnimKey.BeingThrown));
        phys.Velocity = Vector2.zero;
    }

    protected override void OnTick() { }

    // ������ ��(ThrowState���� Carried ����) ���߿� �� �����Ƿ� ����/�ٿ����� �̾���
    protected override void OnExit()
    {
        property.isInputEnabled = true;
        // mode/�߷��� ������ �ʿ��� ���� ������ ����
    }
}
