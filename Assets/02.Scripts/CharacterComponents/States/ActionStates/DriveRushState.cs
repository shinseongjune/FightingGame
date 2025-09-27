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
        // 상태 빠져나갈 때 입력 잠금 해제
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

    // ---- 충돌 이벤트(히트캔슬/가드캔슬 등) ----
    public override void HandleHit(HitData hit)
    {

    }

    public override void HandleGuard(PhysicsEntity atk, PhysicsEntity def, CollisionData cd)
    {

    }

    public override void HandleThrow(PhysicsEntity atk, PhysicsEntity def, CollisionData cd)
    {

    }

    // ---- 중립 복귀 ----
    private void ReturnToNeutral()
    {
        Transition("Idle");
    }
}
