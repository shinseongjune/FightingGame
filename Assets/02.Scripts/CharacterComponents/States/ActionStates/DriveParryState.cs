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

    private FxInstance _parryLoopFx;

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

        // ����Ʈ
        int right = property.isFacingRight ? 1 : -1;
        Quaternion rot = Quaternion.identity;
        rot.eulerAngles = new Vector3(right > 0 ? 0f : 180f, 0f, 0f);
        if (FxService.Instance != null) FxService.Instance.Spawn("DriveParryBase", fsm.transform.position, rot);
    }

    protected override void OnTick()
    {
        if (_phase == ParryPhase.Loop)
        {
            if (property.IsParryLocked)
            {
                // ���� ����: �Է¸� ����, ��ų ĵ��/��Ż ����
                property.isInputEnabled = false;
                property.isSkillCancelable = false;
                // ���⼭�� �ܼ� ���
                return;
            }
            else
            {
                // ������: ���� �Է�/ĵ�� ���
                property.isInputEnabled = true;
                property.isSkillCancelable = true;

                // �и� ���� �Է� ����/������ ����/��Ÿ �������� �ڿ� ������:
                // RequestNaturalExit();
            }
        }

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

        if (_parryLoopFx != null)
        {
            _parryLoopFx.Despawn();
            _parryLoopFx = null;
        }

        RequestNaturalExit();
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
        // ���� ���
        TryPlay(property.characterName + "/" + animCfg.GetClipKey(AnimKey.ParryLoop), null, loop: true);

        // ���� ����Ʈ
        if (FxService.Instance != null)
        {
            // ĳ���� ��Ʈ�� ���̵�, �ʿ��ϸ� ��(��: ����/��) Transform�� ������ ���ʿ� �ٿ��� ����
            var facingRight = property.isFacingRight;
            var localRot = Quaternion.Euler(facingRight ? 0f : 180f, 0f, 0f);

            // ��ġ�� ������. ��¦ ���ʿ� ���� ����
            _parryLoopFx = FxService.Instance.SpawnAttached(
                key: "DriveParryBase",
                parent: fsm.transform,
                localOffset: new Vector3(0f, 1.0f, 0f),
                localRot: localRot
            );
        }
    }

    void RequestNaturalExit()
    {
        if (_phase == ParryPhase.End) return;
        _phase = ParryPhase.End;

        if (_parryLoopFx != null)
        {
            _parryLoopFx.Despawn();
            _parryLoopFx = null;
        }

        // End�� �ܹ߼�
        TryPlay(property.characterName + "/" + animCfg.GetClipKey(AnimKey.ParryEnd), ReturnToNeutral, loop: false);
    }
}
