using UnityEngine;

public class DriveImpactState : CharacterState
{
    private int driveCost = 100;

    private Skill_SO skill;

    public DriveImpactState(CharacterFSM f) : base(f) { }

    public override CharacterStateTag? StateTag => CharacterStateTag.DriveImpact;

    protected override void OnEnter()
    {
        property.attackInstanceId++;

        skill = property.currentSkill;
        if (skill == null) { Transition("Idle"); return; }

        phys.mode = PhysicsMode.Normal;
        phys.isGravityOn = true;
        phys.SetActiveWhiffBoxes(true);

        bool ok = TryPlay(property.characterName + "/" + skill.animationClipName, OnAnimComplete);
        if (!ok)
        {
            UnityEngine.Debug.LogWarning($"[Skill] Animation not found for '{skill.skillName}' : '{skill.animationClipName}'");
            boxApplier.ClearAllBoxes();
            ReturnToNeutral();
            return;
        }

        property.isInputEnabled = false;
        property.isSkillCancelable = false;
        property.superArmorCount = 3;
        property.ConsumeDriveGauge(driveCost);
        property.isDriveGaugeCharging = false;

        // 이펙트
        int right = property.isFacingRight ? 1 : -1;
        Quaternion rot = Quaternion.identity;
        rot.eulerAngles = new Vector3(0f, 0f, right > 0 ? 0f : 180f);
        if (FxService.Instance != null) FxService.Instance.Spawn("DriveImpactBase", fsm.transform.position, rot);

    }

    protected override void OnTick()
    {

    }

    protected override void OnExit()
    {
        // 상태 빠져나갈 때 입력 잠금 해제
        property.isInputEnabled = true;
        property.isSkillCancelable = false;
        property.currentSkill = null;
        property.superArmorCount = 0;
        property.isDriveGaugeCharging = true;

        phys.SetActiveWhiffBoxes(false);
    }

    private void OnAnimComplete()
    {
        ReturnToNeutral();
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
