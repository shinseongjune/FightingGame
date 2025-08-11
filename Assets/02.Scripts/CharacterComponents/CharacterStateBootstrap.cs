using UnityEngine;

[RequireComponent(typeof(CharacterFSM))]
public class CharacterStateBootstrap : MonoBehaviour
{
    void Start()
    {
        var fsm = GetComponent<CharacterFSM>();

        fsm.RegisterState("Idle", new IdleState(fsm));
        fsm.RegisterState("Crouch", new CrouchState(fsm));
        fsm.RegisterState("WalkF", new WalkForwardState(fsm));
        fsm.RegisterState("WalkB", new WalkBackwardState(fsm));

        fsm.RegisterState("JumpU", new JumpUpState(fsm));
        fsm.RegisterState("JumpF", new JumpForwardState(fsm));
        fsm.RegisterState("JumpB", new JumpBackwardState(fsm));
        fsm.RegisterState("Fall", new FallState(fsm));
        fsm.RegisterState("Land", new LandState(fsm));

        fsm.RegisterState("Skill", new SkillPerformState(fsm));
        fsm.RegisterState("HitGround", new HitGroundState(fsm));
        fsm.RegisterState("HitAir", new HitAirState(fsm));
        fsm.RegisterState("GuardHit", new BlockstunState(fsm));

        fsm.RegisterState("Guarding", new GuardingState(fsm));
        fsm.RegisterState("Knockdown", new KnockdownState(fsm));
        fsm.RegisterState("HardKnockdown", new HardKnockdownState(fsm));
        fsm.RegisterState("WakeUp", new WakeUpState(fsm));
        fsm.RegisterState("Throw", new ThrowState(fsm));
        fsm.RegisterState("BeingThrown", new BeingThrownState(fsm));
        fsm.RegisterState("ForcedAnimation", new ForcedAnimationState(fsm));

        fsm.ForceSetState("Idle");
    }
}
