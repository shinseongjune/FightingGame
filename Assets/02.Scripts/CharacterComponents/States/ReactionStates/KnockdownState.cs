// KnockdownState.cs
using UnityEngine;

public class KnockdownState : CharacterState
{
    [SerializeField] int downFrames = 60; // 누운 시간
    int remain;

    public KnockdownState(CharacterFSM f) : base(f) { }
    public override CharacterStateTag? StateTag => CharacterStateTag.Knockdown;

    protected override void OnEnter()
    {
        phys.mode = PhysicsMode.Normal;
        phys.isGravityOn = true;
        phys.SetPose(CharacterStateTag.Knockdown);
        Play(animCfg.GetClipKey(AnimKey.Knockdown));

        property.isInputEnabled = false;
        property.isSkillCancelable = false;

        remain = Mathf.Max(1, downFrames);
        phys.Velocity = Vector2.zero;
    }

    protected override void OnTick()
    {
        if (--remain <= 0)
            Transition("WakeUp");
    }

    protected override void OnExit() { }
}

public class HardKnockdownState : CharacterState
{
    [SerializeField] int downFrames = 90;
    int remain;

    public HardKnockdownState(CharacterFSM f) : base(f) { }
    public override CharacterStateTag? StateTag => CharacterStateTag.HardKnockdown;

    protected override void OnEnter()
    {
        phys.mode = PhysicsMode.Normal;
        phys.isGravityOn = true;
        phys.SetPose(CharacterStateTag.HardKnockdown);
        Play(animCfg.GetClipKey(AnimKey.HardKnockdown));

        property.isInputEnabled = false;
        property.isSkillCancelable = false;
        remain = Mathf.Max(1, downFrames);
        phys.Velocity = Vector2.zero;
    }

    protected override void OnTick()
    {
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

        if (!TryPlay(animCfg.GetClipKey(AnimKey.WakeUp), OnWakeFinish))
        {
            ReturnToNeutralPose();
            return;
        }
    }

    void OnWakeFinish()
    {
        // 입력/자세에 따라 복귀
        ReturnToNeutralPose();
    }

    protected override void OnTick() { }
    protected override void OnExit()
    {
        phys.mode = PhysicsMode.Normal;
        phys.isGravityOn = true;
    }
}
