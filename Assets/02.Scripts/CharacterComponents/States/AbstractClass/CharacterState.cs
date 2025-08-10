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
    protected readonly SkillExecutor skillExec;
    protected readonly BoxPresetApplier boxApplier;
    protected readonly CollisionResolver resolver;

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
        skillExec = fsm.GetComponent<SkillExecutor>();
        boxApplier = fsm.GetComponent<BoxPresetApplier>();
        resolver = fsm.GetComponent<CollisionResolver>();
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

    // ===== (옵션) CollisionResolver 구독 헬퍼 =====
    protected void SubscribeOnEnter()
    {
        if (resolver == null) return;
        resolver.OnHitResolved += HandleHit;
        resolver.OnGuardResolved += HandleGuard;
        resolver.OnThrowResolved += HandleThrow;
    }

    protected void UnsubscribeOnExit()
    {
        if (resolver == null) return;
        resolver.OnHitResolved -= HandleHit;
        resolver.OnGuardResolved -= HandleGuard;
        resolver.OnThrowResolved -= HandleThrow;
    }

    // 필요한 상태만 override
    protected virtual void HandleHit(HitData hit) { }
    protected virtual void HandleGuard(PhysicsEntity atk, PhysicsEntity def, CollisionData cd) { }
    protected virtual void HandleThrow(PhysicsEntity atk, PhysicsEntity def, CollisionData cd) { }
}
