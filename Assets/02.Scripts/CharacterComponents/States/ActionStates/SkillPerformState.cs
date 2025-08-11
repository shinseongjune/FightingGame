public class SkillPerformState : CharacterState
{
    private Skill_SO skill;
    private bool finished;

    // 간단한 예: 특정 프레임부터 캔슬 허용
    // 필요하면 Skill_SO에 창(window) 데이터를 넣어와도 됨.
    private int cancelStartFrame = 0;   // 예: 0이면 즉시 불가
    private int cancelEndFrame = 999; // 충분히 크게

    public SkillPerformState(CharacterFSM f) : base(f) { }

    public override CharacterStateTag? StateTag => CharacterStateTag.Skill;

    protected override void OnEnter()
    {
        finished = false;

        // 전이 직전에 TryStartSkill에서 박아둔 스킬 획득
        skill = property.currentSkill;
        if (skill == null)
        {
            Transition("Idle");
            return;
        }

        // 포즈/물리 모드(지상 기술 기준)
        phys.mode = PhysicsMode.Normal;
        phys.isGravityOn = true;
        phys.SetPose(CharacterStateTag.Skill);

        // 박스/애니 시작
        boxApplier.ApplySkill(skill);
        anim.Play(skill.animationClipName, OnAnimComplete);

        // 상태 중 입력 잠금/캔슬 가능 여부 등 필요 플래그
        property.isInputEnabled = false;
        property.isSkillCancelable = false;
    }

    protected override void OnTick()
    {
        // 1) 캔슬 타이밍 열기(프레임 기반 샘플)
        int f = anim.CurrentFrame;
        property.isSkillCancelable = (f >= cancelStartFrame && f <= cancelEndFrame);

        // 2) 히트캔슬 or 일반 캔슬 입력
        if (!finished && property.isSkillCancelable)
        {
            if (TryStartSkill())     // 입력 인식되면 다음 스킬로 캔슬
                return;
        }

        // 3) 공중 중 기술 중간에 착지해도 강제로 대기하지 않고,
        //    애니 끝에서 ReturnToNeutral을 태움. 필요하면 여기서 착지 시점 처리를 추가.
    }

    protected override void OnExit()
    {
        // 상태 빠져나갈 때 입력 잠금 해제
        property.isInputEnabled = true;
        property.isSkillCancelable = false;

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
    }

    public override void HandleGuard(PhysicsEntity atk, PhysicsEntity def, CollisionData cd)
    {
        // 가드시 전진 캔슬 등 규칙이 있으면 여기서
        property.isSkillCancelable = true;
    }

    public override void HandleThrow(PhysicsEntity atk, PhysicsEntity def, CollisionData cd)
    {
        // 잡기 성립 시에는 보통 강제 전이
        fsm.TransitionTo("Throw"); // 구현되어 있다면
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
