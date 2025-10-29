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

        var buf = input?.inputQueue;
        if (buf == null) return false;

        var matched = InputRecognizer.Recognize(buf, property.allSkills);
        if (matched == null) return false;

        if (!ConditionsPass(property, matched))
        {
            return false;
        }

        if (matched.skillFlag == SkillFlag.DriveParry)
        {
            if (property.parryDisableFrame > 0) return false; // �и� �Ҵ� ������ ��
        }

        // ���̿� ���ؽ�Ʈ ����
        property.currentSkill = matched;
        switch (matched.skillFlag)
        {
            case SkillFlag.None:
                property.characterStateTag = CharacterStateTag.Skill;
                fsm.TransitionTo("Skill");
                break;
            case SkillFlag.DriveImpact:
                property.characterStateTag = CharacterStateTag.DriveImpact;
                fsm.TransitionTo("DriveImpact");
                break;
            case SkillFlag.DriveParry:
                property.characterStateTag = CharacterStateTag.DriveParry;
                fsm.TransitionTo("DriveParry");
                break;
            case SkillFlag.DriveRush:
                property.characterStateTag = CharacterStateTag.DriveRush;
                fsm.TransitionTo("DriveRush");
                break;
            case SkillFlag.DriveReversal:
                property.characterStateTag = CharacterStateTag.DriveReversal;
                fsm.TransitionTo("DriveReversal");
                break;
            default:
                property.characterStateTag = CharacterStateTag.Skill;
                fsm.TransitionTo("Skill");
                break;
        }
        return true;
    }

    protected void ReturnToNeutralPose()
    {
        var d = input?.LastInput.direction ?? Direction.Neutral;

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
        if (anim == null || string.IsNullOrEmpty(clipName))
        {
            Debug.LogWarning($"[State:{Name}] Invalid clip key");
            return;
        }
        if (!anim.Play(clipName, onComplete))
            Debug.LogWarning($"[State:{Name}] Failed to play '{clipName}'");
    }

    protected bool TryPlay(string clipKey, System.Action onComplete = null, bool loop = false)
    {
        if (anim == null || string.IsNullOrEmpty(clipKey)) return false;
        return anim.Play(clipKey, onComplete, loop);
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

    static bool ConditionsPass(CharacterProperty prop, Skill_SO skill)
    {
        var conds = skill.conditions;
        if (conds == null || conds.Length == 0) return true;

        for (int i = 0; i < conds.Length; i++)
        {
            bool ok = true;
            // currentSkill ��ġ �䱸
            if (conds[i].currentSkill != null && prop.currentSkill != conds[i].currentSkill)
            {
                ok = false;
            }

            // ���� �±� ��ġ �䱸
            if (conds[i].currentCharacterState != CharacterStateTag.None &&
                prop.characterStateTag != conds[i].currentCharacterState)
            {
                ok = false;
            }
            // ����̺� �ڽ�Ʈ �䱸
            if (skill.skillFlag != SkillFlag.None && prop.isExhausted)
            {
                ok = false;
            }
            if (ok) return true;
        }
        return false;
    }

    public virtual void OnForceTerminate()
    {
        // TODO: ��ƼŬ/ ���� �� ����
    }

    protected void TryPlayStartVfx(Skill_SO skill)
    {
        if (FxService.Instance == null) return;
        var key = string.IsNullOrEmpty(skill?.startVfxKey) ? FxDefaults.Start() : skill.startVfxKey;
        if (!string.IsNullOrEmpty(key))
            FxService.Instance.Spawn(key, fsm.transform.position, FacingRotation());
    }

    // ���� ������ ī���ʹ� �� ���°� ���� �ִ� ���� (���ٸ� int stateFrame; ����)
    protected void ProcessSkillFxCues(Skill_SO skill, int stateFrame, Transform root, System.Func<string, Transform> boneResolver)
    {
        if (FxService.Instance == null || skill == null || skill.fxCues == null) return;
        // ������ ���ռ�: ���� �����ӿ��� �� ���� �߻� (�ߺ� ����)
        // �� ���¿� HashSet<int> firedFrames; �� �ΰ� �����ص� ��.
        foreach (var cue in skill.fxCues)
        {
            if (cue.frame != stateFrame) continue;
            var t = root;
            if (!string.IsNullOrEmpty(cue.attachBone) && boneResolver != null)
            {
                var bone = boneResolver(cue.attachBone);
                if (bone != null) t = bone;
            }
            var pos = cue.worldSpace ? (t.position + (cue.follow ? t.TransformVector(cue.offset) : cue.offset))
                                     : t.TransformPoint(cue.offset);

            var rot = FacingRotation(); // �¿� ���� ��� (�� ������Ʈ �Ծ�)
            if (!string.IsNullOrEmpty(cue.key))
                FxService.Instance.Spawn(cue.key, pos, rot, -1, null, cue.follow ? t : null);
        }
    }

    protected Quaternion FacingRotation()
    {
        // �ǰ��� ������ �ƴ϶� "�� ����" �¿�. �ʿ� �� CharacterProperty.isFacingRight ����
        var e = Vector3.zero;
        e.y = property.isFacingRight ? 90f : 270f;
        return Quaternion.Euler(e);
    }

    protected void FireSkillStartSFX(Skill_SO s)
    {
        var key = string.IsNullOrEmpty(s.startSfxKey) ? SfxDefaults.Start() : s.startSfxKey;
        SoundService.Instance?.PlayAttached(key, tr); // ĳ���� ����
    }

    protected void ProcessSfxCues(Skill_SO s, int currentFrame)
    {
        if (s.sfxCues == null) return;
        for (int i = 0; i < s.sfxCues.Count; ++i)
        {
            var cue = s.sfxCues[i];
            if (cue.frame != currentFrame) continue;

            Transform follow = tr;
            Vector3 pos = tr.position;

            if (!string.IsNullOrEmpty(cue.attachBone))
            {
                var bone = property.ResolveBoneTransform(cue.attachBone);
                if (bone != null)
                {
                    if (cue.worldSpace) pos = bone.position + cue.offset;
                    follow = cue.follow ? bone : null;
                }
            }
            var key = cue.key;
            if (string.IsNullOrEmpty(key)) continue;

            if (follow != null && cue.follow)
                SoundService.Instance?.PlayAttached(key, follow, cue.volumeMul <= 0 ? 1f : cue.volumeMul, cue.pitchMul <= 0 ? 1f : cue.pitchMul);
            else
                SoundService.Instance?.PlayAt(key, pos, cue.volumeMul <= 0 ? 1f : cue.volumeMul, cue.pitchMul <= 0 ? 1f : cue.pitchMul);
        }
    }
}
