// KnockdownState.cs
using UnityEngine;

public class KnockdownState : CharacterState
{
    [SerializeField] int downFrames = 60; // 누운 시간
    int remain;
    bool landed;

    public KnockdownState(CharacterFSM f) : base(f) { }
    public override CharacterStateTag? StateTag => CharacterStateTag.Knockdown;

    protected override void OnEnter()
    {
        phys.mode = PhysicsMode.Normal;
        phys.isGravityOn = true;

        landed = phys.isGrounded;
        remain = 0; // ← 카운트는 착지 후 시작

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
                remain = downFrames; // ← 이제부터 다운 카운트 시작
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
        phys.mode = PhysicsMode.Kinematic; // 짧게 위치 고정하고 연출
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
        // 입력/자세에 따라 복귀
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
