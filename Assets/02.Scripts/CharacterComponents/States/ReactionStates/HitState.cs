using UnityEngine;

public class HitGroundState : CharacterState
{
    private int remain;
    private bool crouchHit;

    public HitGroundState(CharacterFSM f) : base(f) { }

    public override CharacterStateTag? StateTag => CharacterStateTag.Hit;

    protected override void OnEnter()
    {
        // �Է� ����, ��ųĵ�� ����
        property.isInputEnabled = false;
        property.isSkillCancelable = false;

        // �ڼ�/����
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

        // ����/�˹� ����
        remain = Mathf.Max(1, property.pendingHitstunFrames);
        if (property.pendingKnockback != Vector2.zero)
            phys.Velocity = new Vector2(property.pendingKnockback.x, 0f); // ����: ���� ����

        // �� �� ���� ���
        property.pendingHitstunFrames = 0;
        property.pendingKnockback = Vector2.zero;
    }

    protected override void OnTick()
    {
        // ���� ���� ����� ����(���ϸ� ���� Phys����)
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

        Play(property.characterName + "/" + animCfg.GetClipKey(AnimKey.HitAir));

        remain = Mathf.Max(1, property.pendingHitstunFrames);
        if (property.pendingKnockback != Vector2.zero)
            phys.Velocity = property.pendingKnockback; // ����: XY ��� �ݿ�

        property.pendingHitstunFrames = 0;
        property.pendingKnockback = Vector2.zero;
    }

    protected override void OnTick()
    {
        // ���� Ÿ�̸Ӱ� ������ �����̸� ��� ����
        if (--remain <= 0)
        {
            Transition("Fall"); // �����̸� ���� ���·�
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

    public BlockstunState(CharacterFSM f) : base(f) { }
    public override CharacterStateTag? StateTag => CharacterStateTag.Hit_Guard;

    protected override void OnEnter()
    {
        property.isInputEnabled = false;
        property.isSkillCancelable = false;

        phys.mode = PhysicsMode.Normal;
        phys.isGravityOn = true; // ���� ������ ���� ��� ���� �� ���� ����

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

        // ����� ��¦ �̲�����
        float slideDir = property.isFacingRight ? -1f : +1f;
        phys.Velocity = new Vector2(2.0f * slideDir, 0f);
    }

    protected override void OnTick()
    {
        phys.Velocity = new Vector2(Mathf.MoveTowards(phys.Velocity.x, 0f, 25f * TickMaster.TICK_INTERVAL), 0f);

        if (--remain <= 0)
        {
            // �Է� ������ ���� ���� ����/������ ����������, ������ �ܼ� ����
            Transition("Idle");
        }
    }

    protected override void OnExit()
    {
        property.isInputEnabled = true;
    }
}