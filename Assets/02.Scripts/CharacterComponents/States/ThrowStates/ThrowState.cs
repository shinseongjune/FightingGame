// ThrowStates.cs
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

/// <summary> 시전자(공격자) 쪽 잡기 상태 </summary>
public class ThrowState : CharacterState
{
    PhysicsEntity target;      // 잡힌 대상
    Vector2 holdOffset = new Vector2(0.6f, 0.9f); // 시전자 기준 붙잡는 위치

    bool didImpact, didRelease;

    private Skill_SO _skill;

    public void SetTarget(PhysicsEntity t) => target = t;

    public ThrowState(CharacterFSM f) : base(f) { }
    public override CharacterStateTag? StateTag => CharacterStateTag.Throw;

    protected override void OnEnter()
    {
        // 펜딩 컨텍스트에서 먼저 꺼내서 캐시
        var ctx = property.ConsumePendingThrow();
        _skill = ctx.has ? ctx.skill : property.currentSkill;

        // 마지막 안전망: 기본 잡기 스킬로 대체(에셋 하나 직렬화해두는 걸 추천)
        if (_skill == null)
        {
            ReturnToNeutralPose(); return; 
        }

        var cfg = _skill.throwCfg;
        property.isInputEnabled = false;
        phys.mode = PhysicsMode.Kinematic;
        phys.isGravityOn = false;

        if (target != null)
        {
            if (cfg.useAttachFollow)
            {
                var anchor = property.GetThrowAnchor(cfg.holdAnchorIndex);
                var signX = property.isFacingRight ? 1f : -1f;

                if (anchor != null)
                    target.AttachTo(anchor, Vector2.zero);
                else
                    target.AttachTo(phys, new Vector2(holdOffset.x * signX, holdOffset.y));
            }
            else
            {
                // attach 생략 — 대신 초기 위치를 애니 기준으로 일회성 맞춤
                // 필요하면 루트 정렬
                var defPhys = target.GetComponent<PhysicsEntity>();
                if (defPhys != null)
                {
                    // 시전자 기준으로 살짝 붙이기 정도만
                    float signX = property.isFacingRight ? 1f : -1f;
                    defPhys.Position = phys.Position + new Vector2(0.5f * signX, 0);
                    defPhys.Velocity = Vector2.zero;
                    defPhys.mode = PhysicsMode.Kinematic;
                }
            }
        }

        TryPlay(property.characterName + "/" + _skill.throwAnimationClipName, ReturnToNeutralPose);
    }

    protected override void OnTick()
    {
        var skill = _skill;
        if (skill == null || target == null) return;

        int f = anim.CurrentTickFrame; // AnimationPlayer 이미 지원
        var cfg = skill.throwCfg;
        var defProp = target.GetComponent<CharacterProperty>();
        if (defProp == null) return;

        // 임팩트 프레임: 데미지/게이지/콤보
        if (!didImpact && f >= cfg.impactFrame)
        {
            didImpact = true;
            if (cfg.damage > 0f)
            {
                int attackerId = property.phys != null ? property.phys.GetInstanceID() : 0;
                float finalDamage = defProp.RegisterComboAndScaleDamage(attackerId, cfg.damage);
                defProp.ApplyDamage(finalDamage);
            }
            // 콤보 유예 연장 (연출 길면 더 늘린다)
            defProp.ExtendComboWindow(Mathf.Max(18, cfg.comboLockFrames));
        }

        // 릴리즈 프레임: 손에서 놓고 발사
        if (!didRelease && f >= cfg.releaseFrame)
        {
            didRelease = true;
            var signX = property.isFacingRight ? 1f : -1f;
            var v = new Vector2(cfg.launchVelocity.x * signX, cfg.launchVelocity.y);

            // 충돌 복구 + 발사
            target.ReleaseFromCarry(true, v);

            // 피격자 후속 상태 (BeingThrown 은 onComplete 에서 Knockdown, 혹은 바로 다운을 원하면 여기서)
            if (cfg.hardKnockdown && defProp.fsm.Current is BeingThrownState)
            {
                // 그냥 BeingThrown 유지 후 onComplete에서 다운 전이(아래에서 콜백 추가)
            }
        }
    }

    protected override void OnExit()
    {
        property.isInputEnabled = true;
        phys.mode = PhysicsMode.Normal;
        phys.isGravityOn = true;

        if (target != null)
            target.ReleaseFromCarry(true, Vector2.zero);
    }
}

/// <summary> 피격자(잡힌 쪽) 상태 </summary>
public class BeingThrownState : CharacterState
{
    private Skill_SO _skill;
    CharacterProperty thrower;

    public void SetThrower(CharacterProperty c) => thrower = c;

    public BeingThrownState(CharacterFSM f) : base(f) { }
    public override CharacterStateTag? StateTag => CharacterStateTag.BeingThrown;

    protected override void OnEnter()
    {
        var ctx = property.ConsumePendingThrow();
        if (ctx.has && ctx.throwerProp != null)
        {
            thrower = ctx.throwerProp; // 원래 SetThrower가 하던 일
                                       // 필요하면 여기서 바로 SetFacing, 무적/충돌 플래그, 애니 선택 분기 등
            _skill = ctx.skill;
            property.SetFacing(!thrower.isFacingRight);
        }
        else
        {
            // 안전장치: 없으면 즉시 중립/다운 등으로 복귀
            fsm.TransitionTo("Knockdown");
            return;
        }

        property.isInputEnabled = false;
        phys.mode = PhysicsMode.Carried;
        phys.isGravityOn = false;

        // 시전자를 바라보게 하고 싶다면:
        //if (thrower != null) property.SetFacing(!thrower.isFacingRight);

        // ★ onComplete로 확실히 종료(다운 전이) ★
        if (thrower == null || _skill == null ||
            !TryPlay(thrower.characterName + "/" + _skill.beingThrownAnimationClipName,
                     () => fsm.TransitionTo("Knockdown")))
        {
            phys.mode = PhysicsMode.Normal;
            phys.isGravityOn = true;
            fsm.TransitionTo("Knockdown");
        }
    }

    protected override void OnTick() { }

    // 던져진 뒤(ThrowState에서 Carried 해제) 공중에 떠 있으므로 낙하/다운으로 이어짐
    protected override void OnExit()
    {
        property.isInputEnabled = true;
        // mode/중력은 시전자 쪽에서 해제 시점에 조정
    }
}
