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
    protected readonly BoxPresetApplier boxApplier;
    protected readonly CollisionResolver resolver;
    protected readonly InputBuffer input;
    protected readonly CharacterAnimationConfig animCfg;

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
        boxApplier = fsm.GetComponent<BoxPresetApplier>();
        resolver = fsm.GetComponent<CollisionResolver>();
        input = fsm.GetComponent<InputBuffer>();
        animCfg = fsm.GetComponent<CharacterAnimationConfig>();
    }

    // Neutral/Cancelable ���°� ȣ���� ��ƿ
    protected bool TryStartSkill()
    {
        if (property == null || property.allSkills == null || property.allSkills.Count == 0)
            return false;

        var buf = fsm.GetComponent<InputBuffer>()?.inputQueue;
        if (buf == null) return false;

        var matched = InputRecognizer.Recognize(buf, property.allSkills);
        if (matched == null) return false;

        // ���̿� ���ؽ�Ʈ ����
        property.characterStateTag = CharacterStateTag.Skill;
        property.currentSkill = matched;            // �� CharacterProperty�� �ʵ� �߰�(�Ʒ� 3��)
        fsm.TransitionTo("Skill");
        return true;
    }

    protected void ReturnToNeutralPose()
    {
        var d = fsm.GetComponent<InputBuffer>()?.LastInput.direction ?? Direction.Neutral;

        // ���� ���κ��� Ȯ��
        if (!phys.isGrounded)
        {
            Transition("Fall");
            return;
        }

        // �ɱ� ���� �Է� ���� ��
        if (d is Direction.Down or Direction.DownBack or Direction.DownForward)
        {
            Transition("Crouch");
            return;
        }

        // �� �ܿ� ���ִ� �⺻ ����
        Transition("Idle");
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

    // �ʿ��� ���¸� override
    public virtual void HandleHit(HitData hit) { }
    public virtual void HandleGuard(PhysicsEntity atk, PhysicsEntity def, CollisionData cd) { }
    public virtual void HandleThrow(PhysicsEntity atk, PhysicsEntity def, CollisionData cd) { }
}
