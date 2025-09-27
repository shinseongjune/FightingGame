public class DriveRushState : CharacterState
{
    private bool isCanceled;
    private int driveCost = 100;

    private Skill_SO skill;

    public DriveRushState(CharacterFSM f) : base(f) { }

    public override CharacterStateTag? StateTag => CharacterStateTag.DriveRush;

    protected override void OnEnter()
    {
        isCanceled = false;

        skill = property.currentSkill;
        if (skill == null) { Transition("Idle"); return; }

        phys.mode = PhysicsMode.Normal;
        phys.isGravityOn = true;

        bool ok = TryPlay(property.characterName + "/" + skill.animationClipName, ReturnToNeutral);
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
    }

    protected override void OnTick()
    {
        isCanceled = TryStartSkill();
    }

    protected override void OnExit()
    {
        // ���� �������� �� �Է� ��� ����
        property.isInputEnabled = true;
        property.isSkillCancelable = false;

        if (!isCanceled)
        {
            property.currentSkill = null;

            ReturnToNeutral();
        }
        else
        {
            property.isRushCanceled = true;
        }
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
