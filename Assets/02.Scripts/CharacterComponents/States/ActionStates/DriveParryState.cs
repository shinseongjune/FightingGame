using UnityEngine;

public class DriveParryState : CharacterState
{
    enum ParryPhase { Start, Loop, End }
    ParryPhase _phase;

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

        _phase = ParryPhase.Start;
        property.BeginParryWindow(JUST, HOLD);

        bool ok = TryPlay(property.characterName + "/" + animCfg.GetClipKey(AnimKey.ParryStart), OnParryStartComplete);
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
        // 상태 빠져나갈 때 입력 잠금 해제
        property.isInputEnabled = true;
        property.isSkillCancelable = false;
        property.currentSkill = null;
        property.isDriveGaugeCharging = true;

        property.ClearParryWindow();

        RequestNaturalExit();
    }

    // ---- 충돌 이벤트(히트캔슬/가드캔슬 등) ----
    public override void HandleHit(HitData hit)
    {
        property.ChargeDriveGauge(hitDriveCharge);

        //TODO: 패리사운드, 이펙트, 시간정지, 저스트도 여기서 처리.
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
        Play(property.characterName + "/" + animCfg.GetClipKey(AnimKey.Idle));
    }

    void OnParryStartComplete()
    {
        if (_phase != ParryPhase.Start) return;
        EnterParryLoop();
    }

    void EnterParryLoop()
    {
        _phase = ParryPhase.Loop;
        // 루프 재생
        TryPlay(property.characterName + "/" + animCfg.GetClipKey(AnimKey.ParryLoop), null, loop: true);
    }

    void RequestNaturalExit()
    {
        if (_phase == ParryPhase.End) return;
        _phase = ParryPhase.End;
        // End는 단발성
        TryPlay(property.characterName + "/" + animCfg.GetClipKey(AnimKey.ParryEnd), ReturnToNeutral, loop: false);
    }
}
