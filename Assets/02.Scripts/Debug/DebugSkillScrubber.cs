using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class DebugSkillScrubber : MonoBehaviour
{
    [Header("Target")]
    public CharacterProperty target;             // 대상 캐릭터
    AnimationPlayer anim;
    BoxPresetApplier boxApplier;
    PhysicsEntity phys;
    CharacterFSM fsm;

    [Header("Control")]
    public bool lockGameTime = true;             // 전역 정지(슬로모션 0)
    public bool lockFSMAndInput = true;          // FSM/입력 잠금
    public bool kinematicPose = true;            // 중력 off + Kinematic
    public bool disableResolverDuringScrub = true;

    [Header("Skill/Frames")]
    public Skill_SO skill;
    public int frame;
    public bool playing;
    public int playFps = 60;

    // 내부 스냅샷
    bool _active;
    CharacterState _prevState;
    bool _prevInputEnabled;
    PhysicsMode _prevPhysMode;
    bool _prevGravity;
    Skill_SO _prevSkill;

    float _accum;

    public bool checkTarget;

    void OnEnable()
    {
        if (!target) target = GetComponent<CharacterProperty>();
        if (!target) return;
        anim = target.GetComponent<AnimationPlayer>();
        boxApplier = target.GetComponent<BoxPresetApplier>();
        phys = target.GetComponent<PhysicsEntity>();
        fsm = target.fsm;
    }

    void OnDisable()
    {
        if (_active) Deactivate();
    }

    public void Activate()
    {
        if (!target || !anim || !boxApplier || !skill) return;
        if (_active) return;

        _active = true;

        // 스냅샷
        _prevSkill = target.currentSkill;
        _prevInputEnabled = target.isInputEnabled;
        if (phys) { _prevPhysMode = phys.mode; _prevGravity = phys.isGravityOn; }
        _prevState = fsm?.Current;

        // 잠금들
        if (lockFSMAndInput) target.isInputEnabled = false;
        if (kinematicPose && phys)
        {
            phys.mode = PhysicsMode.Kinematic;
            phys.isGravityOn = false;
            phys.Velocity = Vector2.zero;
        }
        if (lockGameTime && Application.isPlaying && TimeController.Instance)
            TimeController.Instance.ApplySlowMotion(0f, 9999f);

        // 스킬 주입 + 애니 시작
        target.currentSkill = skill;
        anim.Play(target.characterName + "/" + skill.animationClipName, onComplete: null, loop: false);

        frame = 0;
        ApplyFrame();
    }

    public void Deactivate()
    {
        if (!_active) return;
        _active = false;

        // 원복
        if (fsm != null) { /* 상태 강제 전환 필요시 Idle 등으로 */ }
        if (lockFSMAndInput) target.isInputEnabled = _prevInputEnabled;
        if (kinematicPose && phys)
        {
            phys.mode = _prevPhysMode;
            phys.isGravityOn = _prevGravity;
        }
        if (Application.isPlaying && TimeController.Instance)
            TimeController.Instance.ClearAllTemporalEffects();

        target.currentSkill = _prevSkill;

        // 박스 정리
        boxApplier.ClearAllBoxes();
    }

    void Update()
    {
        if (checkTarget)
        {
            if (!target) target = GetComponent<CharacterProperty>();
            if (!target) return;
            anim = target.GetComponent<AnimationPlayer>();
            boxApplier = target.GetComponent<BoxPresetApplier>();
            phys = target.GetComponent<PhysicsEntity>();
            fsm = target.fsm;
        }

        if (!_active || !skill) return;

        // 재생 모드면 playFps 기준으로 프레임 증가
        if (playing && Application.isPlaying)
        {
            _accum += Time.unscaledDeltaTime; // 전역 0이어도 unscaled는 흐름
            float frameDur = 1f / Mathf.Max(1, playFps);
            while (_accum >= frameDur)
            {
                _accum -= frameDur;
                frame++;
                if (frame > anim.ClipLengthFrames) frame = anim.ClipLengthFrames;
                ApplyFrame();
            }
        }
    }

    public void Step(int delta)
    {
        frame = Mathf.Clamp(frame + delta, 0, anim.ClipLengthFrames);
        ApplyFrame();
    }

    public void ApplyFrame()
    {
        if (!anim) return;
        anim.SetFrame(frame);      // ★ 우리가 추가한 API
        // 충돌/스턴은 원치 않음 → Resolver는 호출하지 않고, 박스만 동기화
        if (boxApplier) boxApplier.Tick();
    }

#if UNITY_EDITOR
    // 간단한 IMGUI 패널 (원하면 EditorWindow로 분리)
    void OnGUI()
    {
        if (!_active)
        {
            GUILayout.BeginArea(new Rect(10, 10, 420, 160), "Skill Scrubber", GUI.skin.window);
            skill = (Skill_SO)EditorGUILayout.ObjectField("Skill", skill, typeof(Skill_SO), false);
            if (GUILayout.Button("Activate")) Activate();
            GUILayout.EndArea();
            return;
        }

        GUILayout.BeginArea(new Rect(10, 10, 480, 220), "Skill Scrubber", GUI.skin.window);
        EditorGUILayout.LabelField($"Clip: {anim.CurrentClipName}  (Frames: {anim.ClipLengthFrames})");
        int nf = EditorGUILayout.IntSlider("Frame", frame, 0, anim.ClipLengthFrames);
        if (nf != frame) { frame = nf; ApplyFrame(); }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("<< -10")) Step(-10);
        if (GUILayout.Button("<  -1")) Step(-1);
        playing = GUILayout.Toggle(playing, "▶︎ Play");
        if (GUILayout.Button("+1  >")) Step(+1);
        if (GUILayout.Button("+10 >>")) Step(+10);
        GUILayout.EndHorizontal();

        lockGameTime = EditorGUILayout.Toggle("Lock Game Time", lockGameTime);
        lockFSMAndInput = EditorGUILayout.Toggle("Lock FSM/Input", lockFSMAndInput);
        kinematicPose = EditorGUILayout.Toggle("Kinematic Pose", kinematicPose);
        disableResolverDuringScrub = EditorGUILayout.Toggle("Skip Resolver", disableResolverDuringScrub);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Apply Frame")) ApplyFrame();
        if (GUILayout.Button("Deactivate")) Deactivate();
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }
#endif
}