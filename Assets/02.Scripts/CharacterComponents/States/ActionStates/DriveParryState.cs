public class DriveParryState : CharacterState
{
    private int driveCost = 50;
    private int driveTickCost = 33 / 60;

    private Skill_SO skill;

    public DriveParryState(CharacterFSM f) : base(f) { }

    public override CharacterStateTag? StateTag => CharacterStateTag.DriveParry;

    protected override void OnEnter()
    {
        skill = property.currentSkill;
        if (skill == null) { Transition("Idle"); return; }

        phys.mode = PhysicsMode.Normal;
        phys.isGravityOn = true;

        bool ok = TryPlay(property.characterName + "/" + skill.animationClipName);
        if (!ok)
        {
            UnityEngine.Debug.LogWarning($"[Skill] Animation not found for '{skill.skillName}' : '{skill.animationClipName}'");
            boxApplier.ClearAllBoxes();
            ReturnToNeutral();
            return;
        }

        property.isInputEnabled = false;
        property.isSkillCancelable = false;
        property.ConsumeDriveGauge(driveCost);
        property.isDriveGaugeCharging = false;
    }

    protected override void OnTick()
    {
        property.ConsumeDriveGauge(driveTickCost);
        if (property.isExhausted)
        {
            fsm.TransitionTo("Idle");
        }

        var d = input?.LastInput.attack ?? AttackKey.None;

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

        ReturnToNeutral();
    }

    // ---- �浹 �̺�Ʈ(��Ʈĵ��/����ĵ�� ��) ----
    public override void HandleHit(HitData hit)
    {

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
