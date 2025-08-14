// GuardingState.cs
using UnityEngine;

public class GuardingState : CharacterState
{
    public GuardingState(CharacterFSM f) : base(f) { }
    public override CharacterStateTag? StateTag => CharacterStateTag.Guarding;
    
    bool wasCrouch;

    protected override void OnEnter()
    {
        phys.mode = PhysicsMode.Normal;
        phys.isGravityOn = true;

        var d = input?.LastInput.direction ?? Direction.Neutral;
        bool crouchGuard = d is Direction.Down or Direction.DownBack or Direction.DownForward;

        ApplyGuardPoseAndAnim(crouchGuard);
        wasCrouch = crouchGuard;

        property.isInputEnabled = true;        // 입력은 받되, 공격은 제한할 수도 있음
        property.isSkillCancelable = false;    // 필요 시 규칙으로 열기
    }

    protected override void OnTick()
    {
        var d = input?.LastInput.direction ?? Direction.Neutral;

        // 가드 유지 입력(뒤/뒤아래) 아니면 해제
        bool holdingGuard = d is Direction.Back or Direction.DownBack;
        if (!holdingGuard)
        {
            // 입력에 맞춰 자연스럽게 복귀
            if (!phys.isGrounded) { Transition("Fall"); return; }
            if (d is Direction.Down or Direction.DownBack or Direction.DownForward) { Transition("Crouch"); return; }
            Transition("Idle");
            return;
        }

        // 가드 유지 중엔 서/앉 전환을 실시간 반영
        bool wantCrouch = d is Direction.DownBack; // 뒤아래면 앉가드, 뒤면 서가드
        if (wantCrouch != wasCrouch)
        {
            ApplyGuardPoseAndAnim(wantCrouch);
            wasCrouch = wantCrouch;
        }
    }

    void ApplyGuardPoseAndAnim(bool crouch)
    {
        if (crouch)
        {
            phys.SetPose(CharacterStateTag.Crouch);
            Play(animCfg.GetClipKey(AnimKey.GuardCrouch)); // AnimSet에 매핑 필요
        }
        else
        {
            // '서가드'는 Idle 포즈를 사용 (Guarding 포즈는 SetPose에 없음)
            phys.SetPose(CharacterStateTag.Idle);
            Play(animCfg.GetClipKey(AnimKey.GuardIdle));
        }
    }

    // 가드 중 맞으면 Blockstun으로
    public override void HandleGuard(PhysicsEntity atk, PhysicsEntity def, CollisionData cd)
    {
        fsm.TransitionTo("GuardHit");
    }

    protected override void OnExit() { }
}
