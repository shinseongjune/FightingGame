// GuardingState.cs
using UnityEngine;

public class GuardingState : CharacterState
{
    public GuardingState(CharacterFSM f) : base(f) { }
    public override CharacterStateTag? StateTag => CharacterStateTag.Guarding;

    protected override void OnEnter()
    {
        phys.mode = PhysicsMode.Normal;
        phys.isGravityOn = true;
        phys.SetPose(CharacterStateTag.Guarding);

        Play(animCfg.GetClipKey(AnimKey.GuardIdle));
        property.isInputEnabled = true;        // 입력은 받되, 공격은 제한할 수도 있음
        property.isSkillCancelable = false;    // 필요 시 규칙으로 열기
    }

    protected override void OnTick()
    {
        // 입력 유지 중(back/down-back)이 아니면 해제
        var d = input?.LastInput.direction ?? Direction.Neutral;
        bool guarding = d is Direction.Back or Direction.DownBack;
        if (!guarding)
        {
            // 땅/앉기 유지 판단 후 복귀
            if (!phys.isGrounded) { Transition("Fall"); return; }
            if (d is Direction.Down or Direction.DownBack or Direction.DownForward) { Transition("Crouch"); return; }
            Transition("Idle");
        }
    }

    // 가드 중 맞으면 Blockstun으로
    public override void HandleGuard(PhysicsEntity atk, PhysicsEntity def, CollisionData cd)
    {
        fsm.TransitionTo("Blockstun");
    }

    protected override void OnExit() { }
}
