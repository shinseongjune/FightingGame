using UnityEngine;

public class HitGroundState : CharacterState
{
    private int remain;

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
        phys.SetPose(CharacterStateTag.Hit);

        // �ִ�
        Play(animCfg.GetClipKey(AnimKey.HitGround));
        
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

        Play(animCfg.GetClipKey(AnimKey.HitAir));

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

    public BlockstunState(CharacterFSM f) : base(f) { }
    public override CharacterStateTag? StateTag => CharacterStateTag.Hit_Guard;

    protected override void OnEnter()
    {
        property.isInputEnabled = false;
        property.isSkillCancelable = false;

        phys.mode = PhysicsMode.Normal;
        phys.isGravityOn = true; // ���� ������ ���� ��� ���� �� ���� ����
        phys.SetPose(CharacterStateTag.Guarding);

        Play(animCfg.GetClipKey(AnimKey.GuardHit));

        remain = Mathf.Max(1, property.pendingBlockstunFrames);
        property.pendingBlockstunFrames = 0;

        // ����� ��¦ �̲�����(����)
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