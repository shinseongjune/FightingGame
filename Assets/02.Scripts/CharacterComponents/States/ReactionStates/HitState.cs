using UnityEngine;

public class HitGroundState : CharacterState
{
    private int remain;
    private bool crouchHit;

    private float saGaugeChargeAmount = 10;

    public HitGroundState(CharacterFSM f) : base(f) { }

    public override CharacterStateTag? StateTag => CharacterStateTag.Hit;

    protected override void OnEnter()
    {
        // 러시 중 피격 시 만약을 위해 러시캔슬 취소
        property.isRushCanceled = false;

        // 입력 금지, 스킬캔슬 금지
        property.isInputEnabled = false;
        property.isSkillCancelable = false;

        // 자세/물리
        phys.mode = PhysicsMode.Normal;
        phys.isGravityOn = true;

        var d = input?.LastInput.direction ?? Direction.Neutral;
        crouchHit = phys.isGrounded && (d == Direction.Down || d == Direction.DownBack || d == Direction.DownForward);

        phys.SetPose(crouchHit ? CharacterStateTag.Crouch : CharacterStateTag.Hit);

        string clip = animCfg.animSet != null
            ? animCfg.animSet.GetOrDefault(
                crouchHit ? AnimKey.HitCrouch : AnimKey.HitGround,
                animCfg.GetClipKey(AnimKey.HitGround))
            : animCfg.GetClipKey(AnimKey.HitGround);
        clip = property.characterName + "/" + clip;
        Play(clip);

        // 경직/넉백 적용
        remain = Mathf.Max(1, property.pendingHitstunFrames);
        if (property.pendingKnockback != Vector2.zero)
            phys.Velocity = new Vector2(property.pendingKnockback.x, 0f); // 지상: 수평 위주

        // 한 번 쓰고 비움
        property.pendingHitstunFrames = 0;
        property.pendingKnockback = Vector2.zero;

        property.ChargeSAGauge(saGaugeChargeAmount);
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

    private float saGaugeChargeAmount = 10;

    public HitAirState(CharacterFSM f) : base(f) { }
    public override CharacterStateTag? StateTag => CharacterStateTag.Hit_Air;

    protected override void OnEnter()
    {
        property.isInputEnabled = false;
        property.isSkillCancelable = false;

        phys.mode = PhysicsMode.Normal;
        phys.isGravityOn = true;
        phys.SetPose(CharacterStateTag.Hit_Air);

        Play(property.characterName + "/" + animCfg.GetClipKey(AnimKey.HitAir));

        remain = Mathf.Max(1, property.pendingHitstunFrames);
        if (property.pendingKnockback != Vector2.zero)
            phys.Velocity = property.pendingKnockback; // 공중: XY 모두 반영

        property.pendingHitstunFrames = 0;
        property.pendingKnockback = Vector2.zero;

        property.ChargeSAGauge(saGaugeChargeAmount);
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
    private bool crouchGuard;

    private float saGaugeChargeAmount = 10;

    public BlockstunState(CharacterFSM f) : base(f) { }
    public override CharacterStateTag? StateTag => CharacterStateTag.Hit_Guard;

    protected override void OnEnter()
    {
        property.isInputEnabled = false;
        property.isSkillCancelable = false;

        phys.mode = PhysicsMode.Normal;
        phys.isGravityOn = true; // 공중 가드라면 공중 포즈를 따로 쓸 수도 있음

        var d = input?.LastInput.direction ?? Direction.Neutral;
        crouchGuard = phys.isGrounded && (d == Direction.Down || d == Direction.DownBack || d == Direction.DownForward);

        phys.SetPose(crouchGuard ? CharacterStateTag.Crouch : CharacterStateTag.Idle);

        string clip = animCfg.animSet != null
            ? animCfg.animSet.GetOrDefault(
                crouchGuard ? AnimKey.GuardHitCrouch : AnimKey.GuardHit,
                animCfg.GetClipKey(AnimKey.GuardHit))
            : animCfg.GetClipKey(AnimKey.GuardHit);
        clip = property.characterName + "/" + clip;
        Play(clip);

        remain = Mathf.Max(1, property.pendingBlockstunFrames);
        property.pendingBlockstunFrames = 0;

        property.ChargeSAGauge(saGaugeChargeAmount);
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