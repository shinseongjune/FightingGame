using UnityEngine;

public class HitGroundState : CharacterState
{
    private int remain;

    public HitGroundState(CharacterFSM f) : base(f) { }

    public override CharacterStateTag? StateTag => CharacterStateTag.Hit;

    protected override void OnEnter()
    {
        // 입력 금지, 스킬캔슬 금지
        property.isInputEnabled = false;
        property.isSkillCancelable = false;

        // 자세/물리
        phys.mode = PhysicsMode.Normal;
        phys.isGravityOn = true;
        phys.SetPose(CharacterStateTag.Hit);

        // 애니
        Play(animCfg.GetClipKey(AnimKey.HitGround));
        
        // 경직/넉백 적용
        remain = Mathf.Max(1, property.pendingHitstunFrames);
        if (property.pendingKnockback != Vector2.zero)
            phys.Velocity = new Vector2(property.pendingKnockback.x, 0f); // 지상: 수평 위주

        // 한 번 쓰고 비움
        property.pendingHitstunFrames = 0;
        property.pendingKnockback = Vector2.zero;
    }

    protected override void OnTick()
    {
        // 지상 마찰 비슷한 덤핑(원하면 별도 Phys에서)
        phys.Velocity = new Vector2(Mathf.MoveTowards(phys.Velocity.x, 0f, 20f * TickMaster.TICK_INTERVAL), 0f);

        if (--remain <= 0)
        {
            Transition("Idle");
        }
    }

    protected override void OnExit()
    {
        property.isInputEnabled = true;
    }
}

public class HitAirState : CharacterState
{
    private int remain;

    public HitAirState(CharacterFSM f) : base(f) { }
    public override CharacterStateTag? StateTag => CharacterStateTag.Hit_Air;

    protected override void OnEnter()
    {
        property.isInputEnabled = false;
        property.isSkillCancelable = false;

        phys.mode = PhysicsMode.Normal;
        phys.isGravityOn = true;
        phys.SetPose(CharacterStateTag.Hit_Air);

        Play(animCfg.GetClipKey(AnimKey.HitAir));

        remain = Mathf.Max(1, property.pendingHitstunFrames);
        if (property.pendingKnockback != Vector2.zero)
            phys.Velocity = property.pendingKnockback; // 공중: XY 모두 반영

        property.pendingHitstunFrames = 0;
        property.pendingKnockback = Vector2.zero;
    }

    protected override void OnTick()
    {
        // 경직 타이머가 끝나도 공중이면 계속 낙하
        if (--remain <= 0)
        {
            Transition("Fall"); // 공중이면 낙하 상태로
        }
    }

    protected override void OnExit()
    {
        property.isInputEnabled = true;
    }
}

public class BlockstunState : CharacterState
{
    private int remain;

    public BlockstunState(CharacterFSM f) : base(f) { }
    public override CharacterStateTag? StateTag => CharacterStateTag.Hit_Guard;

    protected override void OnEnter()
    {
        property.isInputEnabled = false;
        property.isSkillCancelable = false;

        phys.mode = PhysicsMode.Normal;
        phys.isGravityOn = true; // 공중 가드라면 공중 포즈를 따로 쓸 수도 있음
        phys.SetPose(CharacterStateTag.Guarding);

        Play(animCfg.GetClipKey(AnimKey.GuardHit));

        remain = Mathf.Max(1, property.pendingBlockstunFrames);
        property.pendingBlockstunFrames = 0;

        // 가드시 살짝 미끄러짐(선택)
        float slideDir = property.isFacingRight ? -1f : +1f;
        phys.Velocity = new Vector2(2.0f * slideDir, 0f);
    }

    protected override void OnTick()
    {
        phys.Velocity = new Vector2(Mathf.MoveTowards(phys.Velocity.x, 0f, 25f * TickMaster.TICK_INTERVAL), 0f);

        if (--remain <= 0)
        {
            // 입력 유지에 따라 가드 지속/해제도 가능하지만, 지금은 단순 복귀
            Transition("Idle");
        }
    }

    protected override void OnExit()
    {
        property.isInputEnabled = true;
    }
}