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
        property.isInputEnabled = true;        // �Է��� �޵�, ������ ������ ���� ����
        property.isSkillCancelable = false;    // �ʿ� �� ��Ģ���� ����
    }

    protected override void OnTick()
    {
        // �Է� ���� ��(back/down-back)�� �ƴϸ� ����
        var d = input?.LastInput.direction ?? Direction.Neutral;
        bool guarding = d is Direction.Back or Direction.DownBack;
        if (!guarding)
        {
            // ��/�ɱ� ���� �Ǵ� �� ����
            if (!phys.isGrounded) { Transition("Fall"); return; }
            if (d is Direction.Down or Direction.DownBack or Direction.DownForward) { Transition("Crouch"); return; }
            Transition("Idle");
        }
    }

    // ���� �� ������ Blockstun����
    public override void HandleGuard(PhysicsEntity atk, PhysicsEntity def, CollisionData cd)
    {
        fsm.TransitionTo("Blockstun");
    }

    protected override void OnExit() { }
}
