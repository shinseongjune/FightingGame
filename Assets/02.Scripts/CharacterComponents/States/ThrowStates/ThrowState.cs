// ThrowStates.cs
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

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


        if (target != null)
        {
            target.mode = PhysicsMode.Carried;
            target.followTarget = phys;
            var signX = property.isFacingRight ? 1f : -1f;
            target.followOffset = new Vector2(holdOffset.x * signX, holdOffset.y);
        }

        if (!TryPlay(property.characterName + "/" + property.currentSkill.throwAnimationClipName, ReturnToNeutralPose))
        {
            // ���� �������� ���ϸ� �����ϰ� ����
            ReturnToNeutralPose();
            return;
        }
    }

    protected override void OnTick() { }

    protected override void OnExit()
    {
        property.isInputEnabled = true;
        phys.mode = PhysicsMode.Normal;
        phys.isGravityOn = true;

        if (target != null)
        {
            target.mode = PhysicsMode.Normal;
            target.followTarget = null;
        }
    }
}

/// <summary> �ǰ���(���� ��) ���� </summary>
public class BeingThrownState : CharacterState
{
    CharacterProperty thrower;

    public void SetTrower(CharacterProperty c) => thrower = c;

    public BeingThrownState(CharacterFSM f) : base(f) { }
    public override CharacterStateTag? StateTag => CharacterStateTag.BeingThrown;

    protected override void OnEnter()
    {
        property.isInputEnabled = false;
        phys.mode = PhysicsMode.Carried; // ThrowState���� followTarget ����
        phys.isGravityOn = false;

        if (thrower == null || thrower.currentSkill == null)
        {
            // ���� ����
            phys.mode = PhysicsMode.Normal;
            phys.isGravityOn = true;
            Transition("Knockdown");
            return;
        }

        if (!TryPlay(property.characterName + "/" + thrower.currentSkill.beingThrownAnimationClipName))
        {
            // Ŭ���� ��� �ּ��� ���°� ���� �ʵ���
            phys.mode = PhysicsMode.Normal;
            phys.isGravityOn = true;
            Transition("Knockdown");
            return;
        }
        phys.Velocity = Vector2.zero;
    }

    protected override void OnTick() { }

    // ������ ��(ThrowState���� Carried ����) ���߿� �� �����Ƿ� ����/�ٿ����� �̾���
    protected override void OnExit()
    {
        property.isInputEnabled = true;
        // mode/�߷��� ������ �ʿ��� ���� ������ ����

        Transition("Knockdown");
    }
}
