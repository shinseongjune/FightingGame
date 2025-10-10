using UnityEngine;

public class DriveParryState : CharacterState
{
    private int driveCost = 100;
    private float driveTickCost = 33f;
    private float TickDt => TickMaster.Instance != null ? TickMaster.TICK_INTERVAL : 1 / 60f;
    private int hitDriveCharge = 100;

    [SerializeField] int minActiveFrames = 6;
    int _remain;

    private Skill_SO skill;

    public DriveParryState(CharacterFSM f) : base(f) { }

    public override CharacterStateTag? StateTag => CharacterStateTag.DriveParry;

    protected override void OnEnter()
    {
        property.parryDisableFrame = 5;
        _remain = minActiveFrames;

        skill = property.currentSkill;
        if (skill == null) { fsm.RequestTransition("Idle"); return; }

        phys.mode = PhysicsMode.Normal;
        phys.isGravityOn = true;

        const int JUST = 5;
        const int HOLD = 18;

        property.BeginParryWindow(JUST, HOLD);

        bool ok = TryPlay(property.characterName + "/" + animCfg.GetClipKey(AnimKey.ParryStart));
        if (!ok)
        {
            Debug.LogWarning($"[Skill] Animation not found for '{skill.skillName}' : '{skill.animationClipName}'");
            boxApplier.ClearAllBoxes();
            ReturnToNeutral();
            return;
        }

        property.isInputEnabled = true;
        property.isSkillCancelable = false;
        property.ConsumeDriveGauge(driveCost);
        property.isDriveGaugeCharging = false;
    }

    protected override void OnTick()
    {
        property.ConsumeDriveGauge(driveTickCost * TickDt);
        if (property.isExhausted)
        {
            fsm.TransitionTo("Idle");
        }

        if (_remain > 0) { _remain--; return; }

        var d = input.LastInput.attack;

        if ( (d & (AttackKey.MP | AttackKey.MK)) != (AttackKey.MP | AttackKey.MK) )
        {
            fsm.TransitionTo("Idle");
        }
    }

    protected override void OnExit()
    {
        // ���� �������� �� �Է� ��� ����
        property.isInputEnabled = true;
        property.isSkillCancelable = false;
        property.currentSkill = null;
        property.isDriveGaugeCharging = true;

        property.ClearParryWindow();
    }

    // ---- �浹 �̺�Ʈ(��Ʈĵ��/����ĵ�� ��) ----
    public override void HandleHit(HitData hit)
    {
        property.ChargeDriveGauge(hitDriveCharge);

        //TODO: �и�����, ����Ʈ, �ð�����, ����Ʈ�� ���⼭ ó��.
    }

    public override void HandleGuard(PhysicsEntity atk, PhysicsEntity def, CollisionData cd)
    {

    }

    public override void HandleThrow(PhysicsEntity atk, PhysicsEntity def, CollisionData cd)
    {

    }

    // ---- �߸� ���� ----
    private void ReturnToNeutral()
    {
        Transition("Idle");
    }
}
