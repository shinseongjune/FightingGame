using UnityEngine;

/// <summary>
/// ��� ĳ���� ������ ���� ���̽�.
/// - �ʼ� ������Ʈ ĳ�� (FSM/Property/Physics/Anim/Skill/Box/Resolver)
/// - ����/ƽ/��Ż + ��� ������ ����
/// - ����/�ִ� ��ƿ
/// - (�ɼ�) CollisionResolver ���� ����
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

    // �ĺ��� �̸�(�����/UI��)
    public virtual string Name => GetType().Name;

    /// <summary>
    /// ���� �±�(enum). ������ �ʿ� ������ null ��ȯ.
    /// �� ���¿��� override �� �ʿ��� �±׸� ����.
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

        // ����: �±׸� �����ϰ� ���� (�ڽ� ������ �� ���¿��� �ʿ� �� ���� ó��)
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
            property.characterStateTag = StateTag.Value; // enum���� �����ϰ� �Ҵ�
        // �ڽ� ��ü�� �ʿ��ϸ� �Ļ� ������ OnEnter���� boxApplier/phys ���� ���� ȣ��
    }

    // ===== (�ɼ�) CollisionResolver ���� ���� =====
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

    // �ʿ��� ���¸� override
    protected virtual void HandleHit(HitData hit) { }
    protected virtual void HandleGuard(PhysicsEntity atk, PhysicsEntity def, CollisionData cd) { }
    protected virtual void HandleThrow(PhysicsEntity atk, PhysicsEntity def, CollisionData cd) { }
}
