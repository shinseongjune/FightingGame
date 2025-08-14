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

        property.isInputEnabled = true;        // �Է��� �޵�, ������ ������ ���� ����
        property.isSkillCancelable = false;    // �ʿ� �� ��Ģ���� ����
    }

    protected override void OnTick()
    {
        var d = input?.LastInput.direction ?? Direction.Neutral;

        // ���� ���� �Է�(��/�ھƷ�) �ƴϸ� ����
        bool holdingGuard = d is Direction.Back or Direction.DownBack;
        if (!holdingGuard)
        {
            // �Է¿� ���� �ڿ������� ����
            if (!phys.isGrounded) { Transition("Fall"); return; }
            if (d is Direction.Down or Direction.DownBack or Direction.DownForward) { Transition("Crouch"); return; }
            Transition("Idle");
            return;
        }

        // ���� ���� �߿� ��/�� ��ȯ�� �ǽð� �ݿ�
        bool wantCrouch = d is Direction.DownBack; // �ھƷ��� �ɰ���, �ڸ� ������
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
            Play(animCfg.GetClipKey(AnimKey.GuardCrouch)); // AnimSet�� ���� �ʿ�
        }
        else
        {
            // '������'�� Idle ��� ��� (Guarding ����� SetPose�� ����)
            phys.SetPose(CharacterStateTag.Idle);
            Play(animCfg.GetClipKey(AnimKey.GuardIdle));
        }
    }

    // ���� �� ������ Blockstun����
    public override void HandleGuard(PhysicsEntity atk, PhysicsEntity def, CollisionData cd)
    {
        fsm.TransitionTo("GuardHit");
    }

    protected override void OnExit() { }
}
