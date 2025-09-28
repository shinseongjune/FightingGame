public class SkillPerformState : CharacterState
{
    private Skill_SO skill;
    private bool finished;

    private bool wasGrounded;

    public bool isRushCanceled;

    // 간단한 예: 특정 프레임부터 캔슬 허용
    // 필요하면 Skill_SO에 창(window) 데이터를 넣어와도 됨.
    private int cancelStartFrame = 0;   // 예: 0이면 즉시 불가
    private int cancelEndFrame = 999; // 충분히 크게

    public SkillPerformState(CharacterFSM f) : base(f) { }

    public override CharacterStateTag? StateTag
    {
        get
        {
            if (isRushCanceled)
                return CharacterStateTag.DriveRushSkill;

            return CharacterStateTag.Skill;
        }
    }

    protected override void OnEnter()
    {
        if (property.isRushCanceled)
        {
            isRushCanceled = true;
            property.isRushCanceled = false;
        }

        property.attackInstanceId++;

        finished = false;

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
        property.isDriveGaugeCharging = false;

        wasGrounded = phys.isGrounded;
    }

    protected override void OnTick()
    {
        if (!wasGrounded && phys.isGrounded)
        {
            fsm.TransitionTo("Idle");
        }

        wasGrounded = phys.isGrounded;

        // 1) 캔슬 타이밍 열기(프레임 기반 샘플)
        int f = anim.CurrentClipFrame;
        property.isSkillCancelable = (f >= cancelStartFrame && f <= cancelEndFrame);

        // 2) 히트캔슬 or 일반 캔슬 입력
        if (!finished && property.isSkillCancelable)
        {
            if (TryStartSkill())     // 입력 인식되면 다음 스킬로 캔슬
                return;
        }
    }

    protected override void OnExit()
    {
        // 상태 빠져나갈 때 입력 잠금 해제
        property.isInputEnabled = true;
        property.isSkillCancelable = false;
        property.currentSkill = null;
        property.isDriveGaugeCharging = true;

        phys.SetActiveWhiffBoxes(false);

        // FSM이 기본적으로 전이 시 BoxPresetApplier.ClearAll()을 호출하고 있으니
        // 여기서 굳이 박스를 지울 필요는 없음.
    }

    private void OnAnimComplete()
    {
        finished = true;
        ReturnToNeutral();
    }

    // ---- 충돌 이벤트(히트캔슬/가드캔슬 등) ----
    public override void HandleHit(HitData hit)
    {
        // 히트 확인 시 즉시 캔슬 창 열기(원하면 조건부)
        property.isSkillCancelable = true;
        property.ChargeDriveGauge(skill.driveGaugeChargeAmount);
    }

    public override void HandleGuard(PhysicsEntity atk, PhysicsEntity def, CollisionData cd)
    {
        // 가드시 전진 캔슬 등 규칙이 있으면 여기서
        property.isSkillCancelable = true;
    }

    public override void HandleThrow(PhysicsEntity atk, PhysicsEntity def, CollisionData cd)
    {

    }

    // ---- 중립 복귀 ----
    private void ReturnToNeutral()
    {
        // 공중이면 낙하, 앉기 유지면 앉기, 그 외 대기
        var d = input?.LastInput.direction ?? Direction.Neutral;

        if (!phys.isGrounded) { Transition("Fall"); return; }

        if (d is Direction.Down or Direction.DownBack or Direction.DownForward)
        { Transition("Crouch"); return; }

        Transition("Idle");
    }
}
