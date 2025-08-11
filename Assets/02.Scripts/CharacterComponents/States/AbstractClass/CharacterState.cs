using UnityEngine;

/// <summary>
/// 모든 캐릭터 상태의 공통 베이스.
/// - 필수 컴포넌트 캐싱 (FSM/Property/Physics/Anim/Skill/Box/Resolver)
/// - 진입/틱/이탈 + 경과 프레임 관리
/// - 전이/애니 유틸
/// - (옵션) CollisionResolver 구독 헬퍼
/// </summary>
public abstract class CharacterState
{
    // Core refs
    protected readonly CharacterFSM fsm;
    protected readonly GameObject go;
    protected readonly Transform tr;

    // Components
    protected readonly CharacterProperty property;
    protected readonly PhysicsEntity phys;
    protected readonly AnimationPlayer anim;
    protected readonly BoxPresetApplier boxApplier;
    protected readonly CollisionResolver resolver;
    protected readonly InputBuffer input;
    protected readonly CharacterAnimationConfig animCfg;

    // Tick info
    protected int elapsedFrames;
    public int ElapsedFrames => elapsedFrames;

    // 식별용 이름(디버그/UI용)
    public virtual string Name => GetType().Name;

    /// <summary>
    /// 상태 태그(enum). 설정할 필요 없으면 null 반환.
    /// 각 상태에서 override 해 필요한 태그만 지정.
    /// </summary>
    public virtual CharacterStateTag? StateTag => null;

    protected CharacterState(CharacterFSM fsm)
    {
        this.fsm = fsm;
        this.go = fsm.gameObject;
        this.tr = fsm.transform;

        property = fsm.GetComponent<CharacterProperty>();
        phys = fsm.GetComponent<PhysicsEntity>();
        anim = fsm.GetComponent<AnimationPlayer>();
        boxApplier = fsm.GetComponent<BoxPresetApplier>();
        resolver = fsm.GetComponent<CollisionResolver>();
        input = fsm.GetComponent<InputBuffer>();
        animCfg = fsm.GetComponent<CharacterAnimationConfig>();
    }

    // Neutral/Cancelable 상태가 호출할 유틸
    protected bool TryStartSkill()
    {
        if (property == null || property.allSkills == null || property.allSkills.Count == 0)
            return false;

        var buf = fsm.GetComponent<InputBuffer>()?.inputQueue;
        if (buf == null) return false;

        var matched = InputRecognizer.Recognize(buf, property.allSkills);
        if (matched == null) return false;

        // 전이용 컨텍스트 저장
        property.characterStateTag = CharacterStateTag.Skill;
        property.currentSkill = matched;            // ▼ CharacterProperty에 필드 추가(아래 3번)
        fsm.TransitionTo("Skill");
        return true;
    }

    protected void ReturnToNeutralPose()
    {
        var d = fsm.GetComponent<InputBuffer>()?.LastInput.direction ?? Direction.Neutral;

        // 착지 여부부터 확인
        if (!phys.isGrounded)
        {
            Transition("Fall");
            return;
        }

        // 앉기 방향 입력 유지 중
        if (d is Direction.Down or Direction.DownBack or Direction.DownForward)
        {
            Transition("Crouch");
            return;
        }

        // 그 외엔 서있는 기본 상태
        Transition("Idle");
    }

    // ===== Lifecycle =====
    public void Enter()
    {
        elapsedFrames = 0;

        // 공통: 태그만 안전하게 세팅 (박스 갱신은 각 상태에서 필요 시 직접 처리)
        ApplyStateTag();

        OnEnter();
    }

    public void Tick()
    {
        OnTick();
        elapsedFrames++;
    }

    public void Exit()
    {
        OnExit();
    }

    // ===== Abstracts =====
    protected abstract void OnEnter();
    protected abstract void OnTick();
    protected abstract void OnExit();

    // ===== Helpers =====
    protected void Transition(string stateKey) => fsm.TransitionTo(stateKey);

    protected void Play(string clipName, System.Action onComplete = null)
    {
        if (anim != null && !string.IsNullOrEmpty(clipName))
            anim.Play(clipName, onComplete);
    }

    protected bool Reached(int frames) => elapsedFrames >= frames;

    protected virtual void ApplyStateTag()
    {
        if (property != null && StateTag.HasValue)
            property.characterStateTag = StateTag.Value; // enum으로 안전하게 할당
        // 박스 교체가 필요하면 파생 상태의 OnEnter에서 boxApplier/phys 등을 직접 호출
    }

    // 필요한 상태만 override
    public virtual void HandleHit(HitData hit) { }
    public virtual void HandleGuard(PhysicsEntity atk, PhysicsEntity def, CollisionData cd) { }
    public virtual void HandleThrow(PhysicsEntity atk, PhysicsEntity def, CollisionData cd) { }
}
