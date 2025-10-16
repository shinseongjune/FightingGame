// KnockdownState.cs
using UnityEngine;

public class KnockdownState : CharacterState
{
    [SerializeField] int downFrames = 60; // ���� �ð�
    int remain;

    bool wasGrounded;

    public KnockdownState(CharacterFSM f) : base(f) { }
    public override CharacterStateTag? StateTag => CharacterStateTag.Knockdown;

    protected override void OnEnter()
    {
        phys.mode = PhysicsMode.Normal;
        phys.isGravityOn = true;
        phys.SetPose(CharacterStateTag.Knockdown);
        Play(property.characterName + "/" + animCfg.GetClipKey(AnimKey.Knockdown));

        property.isInputEnabled = false;
        property.isSkillCancelable = false;

        remain = Mathf.Max(1, downFrames);
        phys.Velocity = Vector2.zero;
    }

    protected override void OnTick()
    {
        if (!phys.isGrounded)
        {
            return;
        }
        else
        {
            if (!wasGrounded)
            {
                Play(property.characterName + "/" + animCfg.GetClipKey(AnimKey.KnockdownGround));
            }
            if (--remain <= 0)
                Transition("WakeUp");
        }

        wasGrounded = phys.isGrounded;
    }

    protected override void OnExit() { }
}

public class HardKnockdownState : CharacterState
{
    [SerializeField] int downFrames = 90;
    int remain;

    bool wasGrounded;

    public HardKnockdownState(CharacterFSM f) : base(f) { }
    public override CharacterStateTag? StateTag => CharacterStateTag.HardKnockdown;

    protected override void OnEnter()
    {
        phys.mode = PhysicsMode.Normal;
        phys.isGravityOn = true;
        phys.SetPose(CharacterStateTag.HardKnockdown);
        Play(property.characterName + "/" + animCfg.GetClipKey(AnimKey.HardKnockdown));

        property.isInputEnabled = false;
        property.isSkillCancelable = false;
        remain = Mathf.Max(1, downFrames);
        phys.Velocity = Vector2.zero;
    }

    protected override void OnTick()
    {
        if (!phys.isGrounded)
        {
            return;
        }
        else
        {
            if (!wasGrounded)
            {
                Play(property.characterName + "/" + animCfg.GetClipKey(AnimKey.KnockdownGround));
            }
            if (--remain <= 0)
                Transition("WakeUp");
        }

        wasGrounded = phys.isGrounded;
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
    }

    protected override void OnTick() { }
    protected override void OnExit()
    {
        phys.mode = PhysicsMode.Normal;
        phys.isGravityOn = true;
    }
}
