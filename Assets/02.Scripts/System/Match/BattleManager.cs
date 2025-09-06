// GameManager.cs
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR && HAS_ADDRESSABLES
using UnityEditor;
using UnityEditor.AddressableAssets;
#endif

public enum MatchState { None, PreRound, Playing, RoundOver, MatchOver, Paused }

public class BattleManager : MonoBehaviour, ITicker
{
    [Header("Rules")]
    [Tooltip("있으면 시작 시 preset.data를 activeRules로 복사만 합니다.")]
    public MatchRules_SO preset;
    public bool usePresetOnStart = true;
    public MatchRulesData activeRules = MatchRulesData.Default;

    [Header("Refs")]
    public RoundController round;
    public CharacterProperty p1;
    public CharacterProperty p2;

    [Header("Score")]
    public int p1Wins { get; private set; }
    public int p2Wins { get; private set; }

    [Header("Debug")]
    public bool debugAuto = false;           // DebugAutoDriver들이 이 값을 참조하도록
    public bool autoStartMatch = true;
    public Key pauseKey = Key.F10;         // 플레이 중에만 동작
    public Key stepKey = Key.F11;         // Paused일 때 1틱 진행
    public Key restartKey = Key.F12;         // 매치 리스타트

    public MatchState State { get; private set; } = MatchState.None;

    int freezeTicks;                         // PreRound/KO 연출 대기용
    InputAction pauseAct, stepAct, restartAct;

    [Header("Catalog & IDs")]
    public CharacterCatalog_SO catalog;
    public string p1Id = "TestMan";
    public string p2Id = "TestMan2";
    public Transform characterParent;    // 씬 정리용

    // 스폰된 런타임 GO/Prop
    GameObject p1GO, p2GO;

    [Header("Animation Preload (Addressables)")]
    public bool preloadAnims = true;

    // 두 가지 입력 방식 중 편한 걸 쓰면 돼요.
    [Tooltip("여기에 여러 줄로 Addressables 키를 붙여넣기 (줄바꿈/쉼표 구분)")]
    [TextArea(3, 8)] public string preloadAnimKeysText;

    [Tooltip("리스트로도 추가하고 싶으면 여기 사용")]
    public List<string> preloadAnimKeys = new();

    // (선택) 카탈로그 키도 같이 예열하고 싶을 때 편의 옵션
    [Tooltip("P1/P2의 CharacterCatalog clipKeys도 함께 예열")]
    public bool includeCatalogClipsForP1P2 = false;

    [Tooltip("카탈로그의 모든 캐릭터 clipKeys까지 전부 예열(테스트용)")]
    public bool includeAllCatalogClips = false;

    [Header("Animation Preload (Drag & Drop Clips)")]
    [Tooltip("여기에 AnimationClip 에셋들을 드래그하면, Addressables 주소로 자동 변환되어 예열 목록(preloadAnimKeys)에 추가됩니다.")]
    public List<AnimationClip> preloadAnimClips = new();

    // ---------- Unity / Tick ----------
    void Awake()
    {
        if (!round) round = GetComponent<RoundController>();
        if (!p1 || !p2) Debug.LogWarning("[GameManager] p1/p2 참조가 비어 있음");
        if (!round) Debug.LogWarning("[GameManager] RoundController 참조가 비어 있음");
    }

    void OnEnable()
    {
        TickMaster.Instance?.Register(this);

        if (usePresetOnStart && preset != null)
            activeRules = preset.data;

        if (round != null) round.OnRoundEnd += HandleRoundEnd;

        if (Application.isPlaying)
        {
            pauseAct = new InputAction(type: InputActionType.Button, binding: $"<Keyboard>/{pauseKey.ToString().ToLower()}");
            stepAct = new InputAction(type: InputActionType.Button, binding: $"<Keyboard>/{stepKey.ToString().ToLower()}");
            restartAct = new InputAction(type: InputActionType.Button, binding: $"<Keyboard>/{restartKey.ToString().ToLower()}");
            pauseAct.Enable(); stepAct.Enable(); restartAct.Enable();
        }

        if (autoStartMatch) StartMatch();
    }

    void OnDisable()
    {
        TickMaster.Instance?.Unregister(this);
        if (round != null) round.OnRoundEnd -= HandleRoundEnd;

        pauseAct?.Disable(); stepAct?.Disable(); restartAct?.Disable();
        pauseAct?.Dispose(); stepAct?.Dispose(); restartAct?.Dispose();
        pauseAct = stepAct = restartAct = null;
    }

    public void Tick()
    {
        // 디버그 키(플레이 모드에만)
        if (Application.isPlaying)
        {
            if (pauseAct != null && pauseAct.WasPerformedThisFrame())
                TogglePause();

            if (restartAct != null && restartAct.WasPerformedThisFrame())
                StartMatch(); // 전체 리스타트

            if (State == MatchState.Paused && stepAct != null && stepAct.WasPerformedThisFrame())
                StepOneTick();
        }

        // 상태머신
        switch (State)
        {
            case MatchState.PreRound:
                if (--freezeTicks <= 0) BeginPlaying();
                break;

            case MatchState.RoundOver:
                if (--freezeTicks <= 0) NextAfterRound();
                break;

            case MatchState.Paused:
            case MatchState.Playing:
            case MatchState.MatchOver:
            case MatchState.None:
            default:
                break;
        }
    }

    // ---------- Public API ----------

    public void SetRules(MatchRulesData newRules, bool applyImmediately = false)
    {
        activeRules = newRules;

        if (applyImmediately && State == MatchState.Playing)
        {
            // 즉시 적용은 난폭할 수 있음 → 라운드 강제 종료 후 다음 라운드부터 새 룰 적용
            round?.StopRound();
            HandleRoundEnd(p1Win: true); // 임시 연출(필요 시 연출 코드에 맞게 변경)
        }
    }

    public void TogglePause()
    {
        if (State == MatchState.Paused)
        {
            // 재개
            if (round != null && !round.roundActive && State != MatchState.MatchOver)
                BeginPlaying();
            else
                State = MatchState.Playing;
        }
        else
        {
            State = MatchState.Paused;
        }
    }

    // ---------- Flow ----------
    void StartRoundFlow()
    {
        // 스폰 및 리셋
        ResetPlayer(p1, activeRules.p1Spawn, activeRules.p1FacingRight);
        ResetPlayer(p2, activeRules.p2Spawn, activeRules.p2FacingRight);

        // 입력 잠금
        SetInputEnabled(false);

        // 프리라운드 연출
        freezeTicks = Mathf.Max(0, activeRules.preRoundFreezeTicks);
        State = MatchState.PreRound;
    }

    void BeginPlaying()
    {
        if (round == null) { Debug.LogError("[GameManager] RoundController 없음"); return; }

        SetInputEnabled(true);
        round.StartRound(activeRules.roundTimerSeconds);
        State = MatchState.Playing;
    }

    void HandleRoundEnd(bool p1Win)
    {
        State = MatchState.RoundOver;
        SetInputEnabled(false);

        if (p1Win) p1Wins++;
        else p2Wins++;

        freezeTicks = Mathf.Max(0, activeRules.koFreezeTicks);
    }

    void NextAfterRound()
    {
        // 매치 종료?
        if (p1Wins >= activeRules.winTarget || p2Wins >= activeRules.winTarget)
        {
            State = MatchState.MatchOver;
            // TODO: 결과 연출/씬 전환/리플레이 등
            return;
        }

        // 다음 라운드
        StartRoundFlow();
    }

    void StepOneTick()
    {
        // Paused 상태에서 “한 틱만” 진행하고 다시 정지
        // (여기선 GameManager 내부 로직만 한 틱 진행 — 전역 퍼징은 필요 시 TickMaster 레벨에서 별도 구현)
        switch (State)
        {
            case MatchState.PreRound:
                if (--freezeTicks <= 0) BeginPlaying();
                break;
            case MatchState.RoundOver:
                if (--freezeTicks <= 0) NextAfterRound();
                break;
            case MatchState.Playing:
            default:
                // Playing을 진짜 스텝하려면 TickMaster에 전역 Pause/Step이 있어야 함(추후 확장)
                break;
        }
    }

    // ---------- Utils ----------
    void ResetPlayer(CharacterProperty prop, Vector2 spawn, bool facingRight)
    {
        if (prop == null) return;

        var phys = prop.GetComponent<PhysicsEntity>();
        var fsm = prop.GetComponent<CharacterFSM>();
        var box = prop.GetComponent<BoxPresetApplier>();

        // HP/게이지/상태
        prop.hp = prop.maxHp;
        prop.saGauge = 0;
        prop.driveGauge = 0;
        prop.pendingHitstunFrames = 0;
        prop.pendingBlockstunFrames = 0;
        prop.attackInstanceId = 0;
        prop.currentSkill = null;

        // 방향/위치
        prop.SpawnAt(spawn, facingRight);
        //prop.isFacingRight = facingRight;
        //if (phys != null)
        //{
        //    phys.Position = spawn;
        //    phys.Velocity = Vector2.zero;
        //    phys.isGravityOn = true;
        //    phys.SyncTransform();
        //}

        // 박스 초기화 + 상태 Idle
        if (box != null) box.ClearAllBoxes();
        if (fsm != null) fsm.TransitionTo("Idle");
    }

    void SetInputEnabled(bool on)
    {
        if (p1 != null) p1.isInputEnabled = on;
        if (p2 != null) p2.isInputEnabled = on;
    }

    public void StartMatch() // 기존 StartMatch 대체/보강
    {
        p1Wins = p2Wins = 0;
        StartCoroutine(Co_StartMatch());
    }

    IEnumerator Co_StartMatch()
    {
        if (preloadAnims)
            yield return Co_PreloadAnimationClips();

        // 1) 필요 시 로드&스폰
        if (p1GO == null)
        {
            yield return CharacterLoader.LoadAndSpawn(
                p1Id, catalog, activeRules.p1Spawn, activeRules.p1FacingRight, characterParent,
                (go, prop) => { p1GO = go; p1 = prop; },
                err => Debug.LogError("[GM] P1 load fail: " + err)
            );
        }
        if (p2GO == null)
        {
            yield return CharacterLoader.LoadAndSpawn(
                p2Id, catalog, activeRules.p2Spawn, activeRules.p2FacingRight, characterParent,
                (go, prop) => { p2GO = go; p2 = prop; },
                err => Debug.LogError("[GM] P2 load fail: " + err)
            );
        }

        // 2) 라운드 컨트롤러/ HUD 등 참조 보정
        round.p1 = p1; round.p2 = p2;
        // HUD들도 p1/p2/gm/rc 참조 연결(필요 시 FindObjectOfType로 자동화 가능)

        // 3) 라운드 시작
        StartRoundFlow();
    }

    // 캐릭터 교체(선택)
    public void SwapCharacter(bool isP1, string newId)
    {
        if (isP1) { p1Id = newId; StartCoroutine(Co_Respawn(true)); }
        else { p2Id = newId; StartCoroutine(Co_Respawn(false)); }
    }

    IEnumerator Co_Respawn(bool isP1)
    {
        // 기존 삭제
        if (isP1 && p1GO) { yield return CharacterLoader.Despawn(p1GO); p1GO = null; p1 = null; }
        if (!isP1 && p2GO) { yield return CharacterLoader.Despawn(p2GO); p2GO = null; p2 = null; }

        // 새 스폰
        if (isP1)
        {
            yield return CharacterLoader.LoadAndSpawn(
                p1Id, catalog, activeRules.p1Spawn, activeRules.p1FacingRight, characterParent,
                (go, prop) => { p1GO = go; p1 = prop; });
        }
        else
        {
            yield return CharacterLoader.LoadAndSpawn(
                p2Id, catalog, activeRules.p2Spawn, activeRules.p2FacingRight, characterParent,
                (go, prop) => { p2GO = go; p2 = prop; });
        }

        // 다음 라운드부터 새 캐릭터로 진행
    }

    IEnumerator Co_PreloadAnimationClips()
    {
#if HAS_ADDRESSABLES
    var keys = BuildPreloadKeyList();
    if (keys.Count == 0) yield break;

    // AnimationClipLibrary에 로드 요청
    var task = AnimationClipLibrary.Instance.LoadAssetsAsync(keys);
    while (!task.IsCompleted)
    {
        // 원하면 진행률 표시도 가능: AnimationClipLibrary.Instance.LoadProgress
        yield return null;
    }
#else
        // Addressables 패키지가 없으면 조용히 스킵
        yield break;
#endif
    }

    List<string> BuildPreloadKeyList()
    {
        var set = new HashSet<string>();

        // 1) 멀티라인 텍스트 파싱 (쉼표/세미콜론/줄바꿈 모두 구분자로 처리)
        if (!string.IsNullOrWhiteSpace(preloadAnimKeysText))
        {
            var tokens = preloadAnimKeysText
                .Split(new[] { '\r', '\n', ',', ';' }, System.StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim());
            foreach (var t in tokens) if (!string.IsNullOrEmpty(t)) set.Add(t);
        }

        // 2) 리스트 필드
        if (preloadAnimKeys != null)
            foreach (var k in preloadAnimKeys)
                if (!string.IsNullOrWhiteSpace(k)) set.Add(k.Trim());

        // 3) 카탈로그 편의 옵션
        if (includeCatalogClipsForP1P2 && catalog != null)
        {
            if (catalog.TryGet(p1Id, out var e1) && e1.clipKeys != null)
                foreach (var k in e1.clipKeys) if (!string.IsNullOrWhiteSpace(k)) set.Add(k.Trim());

            if (catalog.TryGet(p2Id, out var e2) && e2.clipKeys != null)
                foreach (var k in e2.clipKeys) if (!string.IsNullOrWhiteSpace(k)) set.Add(k.Trim());
        }

        if (includeAllCatalogClips && catalog != null && catalog.entries != null)
        {
            foreach (var e in catalog.entries)
                if (e.clipKeys != null)
                    foreach (var k in e.clipKeys)
                        if (!string.IsNullOrWhiteSpace(k)) set.Add(k.Trim());
        }

        return set.ToList();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
#if HAS_ADDRESSABLES
        // 인스펙터에서 값 바뀔 때마다 동기화(원하면 주석처리하고 ContextMenu만 써도 됨)
        TrySyncPreloadKeysFromClips();
#endif
    }

#if HAS_ADDRESSABLES
    [ContextMenu("Sync Preload Keys From Clips")]
    public void SyncPreloadKeysFromClips_Menu()
    {
        TrySyncPreloadKeysFromClips();
    }

    void TrySyncPreloadKeysFromClips()
    {
        if (preloadAnimClips == null || preloadAnimClips.Count == 0) return;

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogWarning("[GameManager] AddressableAssetSettings 가 없음. (Window > Asset Management > Addressables > Groups 에서 생성하세요)");
            return;
        }

        if (preloadAnimKeys == null) preloadAnimKeys = new List<string>();
        var set = new HashSet<string>(preloadAnimKeys);

        foreach (var clip in preloadAnimClips)
        {
            if (clip == null) continue;

            string path = AssetDatabase.GetAssetPath(clip);
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning($"[GameManager] {clip.name}: GUID를 찾을 수 없음({path}).");
                continue;
            }

            var entry = settings.FindAssetEntry(guid);
            if (entry == null)
            {
                // Addressables 미지정 자산이면 경고만 — 자동 등록 원하면 아래 주석 해제
                Debug.LogWarning($"[GameManager] {clip.name} 은(는) Addressable 이 아닙니다. Groups 창에서 체크하고 주소를 지정하세요.");
                // // 자동으로 Addressables에 등록하고 기본 그룹으로 이동하려면:
                // var group = settings.DefaultGroup ?? settings.groups.FirstOrDefault();
                // entry = settings.CreateOrMoveEntry(guid, group, readOnly:false, postEvent:true);
                // entry.address = path; // 또는 원하는 명명 규칙
            }

            if (entry != null)
            {
                if (set.Add(entry.address))
                    preloadAnimKeys.Add(entry.address);
            }
        }

        // 중복 제거 결과가 인스펙터에 반영되도록 표시 갱신
        EditorUtility.SetDirty(this);
    }
#endif
#endif
}
