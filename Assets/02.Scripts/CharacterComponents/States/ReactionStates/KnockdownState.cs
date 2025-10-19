// KnockdownState.cs
using UnityEngine;

public class KnockdownState : CharacterState
{
    [SerializeField] int downFrames = 60; // ���� �ð�
    int remain;
    bool landed;

    public KnockdownState(CharacterFSM f) : base(f) { }
    public override CharacterStateTag? StateTag => CharacterStateTag.Knockdown;

    protected override void OnEnter()
    {
        phys.mode = PhysicsMode.Normal;
        phys.isGravityOn = true;

        landed = phys.isGrounded;
        remain = 0; // �� ī��Ʈ�� ���� �� ����

        if (!landed)
            Play(property.characterName + "/" + animCfg.GetClipKey(AnimKey.Knockdown));
        else
        {
            Play(property.characterName + "/" + animCfg.GetClipKey(AnimKey.KnockdownGround));
            remain = downFrames;
        }
    }

    protected override void OnTick()
    {
        if (!landed)
        {
            if (phys.isGrounded)
            {
                landed = true;
                Play(property.characterName + "/" + animCfg.GetClipKey(AnimKey.KnockdownGround));
                remain = downFrames; // �� �������� �ٿ� ī��Ʈ ����
            }
            return;
        }

        if (--remain <= 0)
            Transition("WakeUp");
    }

    protected override void OnExit() { }
}

public class HardKnockdownState : CharacterState
{
    [SerializeField] int downFrames = 90;
    int remain;
    bool landed;

    public HardKnockdownState(CharacterFSM f) : base(f) { }
    public override CharacterStateTag? StateTag => CharacterStateTag.HardKnockdown;

    protected override void OnEnter()
    {
        phys.mode = PhysicsMode.Normal;
        phys.isGravityOn = true;
        phys.SetPose(CharacterStateTag.HardKnockdown);

        property.isInputEnabled = false;
        property.isSkillCancelable = false;
        phys.Velocity = Vector2.zero;

        landed = phys.isGrounded;
        remain = 0;
        if (!landed)
            Play(property.characterName + "/" + animCfg.GetClipKey(AnimKey.HardKnockdown));
        else
        {
            Play(property.characterName + "/" + animCfg.GetClipKey(AnimKey.KnockdownGround));
            remain = downFrames;
        }
    }

    protected override void OnTick()
    {
        if (!landed)
        {
            if (phys.isGrounded)
            {
                landed = true;
                Play(property.characterName + "/" + animCfg.GetClipKey(AnimKey.KnockdownGround));
                remain = downFrames;
            }
            return;
        }

        if (--remain <= 0)
            Transition("WakeUp");
    }

    protected override void OnExit() { }
}

public class WakeUpState : CharacterState
{
    public WakeUpState(CharacterFSM f) : base(f) { }
    public override CharacterStateTag? StateTag => CharacterStateTag.Idle;

    protected override void OnEnter()
    {
        phys.mode = PhysicsMode.Kinematic; // ª�� ��ġ �����ϰ� ����
        phys.isGravityOn = false;

        phys.SetPose(CharacterStateTag.Knockdown);
        property.isInputEnabled = false;

        if (!TryPlay(property.characterName + "/" + animCfg.GetClipKey(AnimKey.WakeUp), OnWakeFinish))
        {
            ReturnToNeutralPose();
            return;
        }
    }

    void OnWakeFinish()
    {
        // �Է�/�ڼ��� ���� ����
        ReturnToNeutralPose();
        property.isInputEnabled = true;
    }

    protected override void OnTick() { }
    protected override void OnExit()
    {
        phys.mode = PhysicsMode.Normal;
        phys.isGravityOn = true;
    }
}
