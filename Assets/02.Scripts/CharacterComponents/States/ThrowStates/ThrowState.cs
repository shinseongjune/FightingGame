// ThrowStates.cs
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

/// <summary> 시전자(공격자) 쪽 잡기 상태 </summary>
public class ThrowState : CharacterState
{
    PhysicsEntity target;      // 잡힌 대상
    Vector2 holdOffset = new Vector2(0.6f, 0.9f); // 시전자 기준 붙잡는 위치

    public void SetTarget(PhysicsEntity t) => target = t;

    public ThrowState(CharacterFSM f) : base(f) { }
    public override CharacterStateTag? StateTag => CharacterStateTag.Throw;

    protected override void OnEnter()
    {
        property.isInputEnabled = false;
        phys.mode = PhysicsMode.Kinematic;   // 연출 동안 루트 제어
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
            // 연출 시작조차 못하면 깨끗하게 복귀
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

/// <summary> 피격자(잡힌 쪽) 상태 </summary>
public class BeingThrownState : CharacterState
{
    CharacterProperty thrower;

    public void SetTrower(CharacterProperty c) => thrower = c;

    public BeingThrownState(CharacterFSM f) : base(f) { }
    public override CharacterStateTag? StateTag => CharacterStateTag.BeingThrown;

    protected override void OnEnter()
    {
        property.isInputEnabled = false;
        phys.mode = PhysicsMode.Carried; // ThrowState에서 followTarget 세팅
        phys.isGravityOn = false;

        if (thrower == null || thrower.currentSkill == null)
        {
            // 안전 복구
            phys.mode = PhysicsMode.Normal;
            phys.isGravityOn = true;
            Transition("Knockdown");
            return;
        }

        if (!TryPlay(property.characterName + "/" + thrower.currentSkill.beingThrownAnimationClipName))
        {
            // 클립이 없어도 최소한 상태가 굳지 않도록
            phys.mode = PhysicsMode.Normal;
            phys.isGravityOn = true;
            Transition("Knockdown");
            return;
        }
        phys.Velocity = Vector2.zero;
    }

    protected override void OnTick() { }

    // 던져진 뒤(ThrowState에서 Carried 해제) 공중에 떠 있으므로 낙하/다운으로 이어짐
    protected override void OnExit()
    {
        property.isInputEnabled = true;
        // mode/중력은 시전자 쪽에서 해제 시점에 조정

        Transition("Knockdown");
    }
}
