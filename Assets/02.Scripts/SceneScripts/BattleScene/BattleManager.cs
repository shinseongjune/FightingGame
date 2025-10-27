using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public static BattleManager I { get; private set; }

    [Header("Active Fighters (only real players, no projectiles)")]
    public CharacterProperty player1;   // P1
    public CharacterProperty player2;   // P2

    [SerializeField] private Transform stageRoot;
    [SerializeField] private Transform p1Spawn;
    [SerializeField] private Transform p2Spawn;
    [SerializeField] private RoundController roundController;
    [SerializeField] private CameraRig_25D cameraRig;
    [SerializeField] private HPBar hpBar_p1;
    [SerializeField] private HPBar hpBar_p2;
    [SerializeField] private DriveBar driveBar_p1;
    [SerializeField] private DriveBar driveBar_p2;
    [SerializeField] private SABar saBar_p1;
    [SerializeField] private SABar saBar_p2;
    [SerializeField] private Combos combos_p1;
    [SerializeField] private Combos combos_p2;

    GameObject stageGO, p1GO, p2GO;

    private NetworkInputProvider _sharedNetProvider;

    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;

        Time.timeScale = 1f;
        GameManager.Instance.actions.Player.Enable();
        GameManager.Instance.actions.Select.Disable();
    }

    // === 등록/해제 ===
    public void RegisterFighter(CharacterProperty prop, int slot /* 1 or 2 */)
    {
        if (prop == null) return;
        if (slot == 1) player1 = prop;
        else player2 = prop;
    }
    public void UnregisterFighter(CharacterProperty prop)
    {
        if (player1 == prop) player1 = null;
        if (player2 == prop) player2 = null;
    }

    // === 조회 헬퍼 ===
    public CharacterProperty GetOpponentOf(CharacterProperty me)
    {
        if (me == null) return null;
        if (player1 == me) return player2;
        return player1;
    }

    public IEnumerable<CharacterProperty> EnumerateFighters()
    {
        if (player1 != null) yield return player1;
        if (player2 != null) yield return player2;
    }

    void Start()
    {
        var m = GameManager.Instance.matchConfig;
        string stageKey = $"Stage/{m.stageId}";
        string p1Key = $"Character/{m.p1.characterId}/{m.p1.costumeId}";
        string p2Key = $"Character/{m.p2.characterId}/{m.p2.costumeId}";

        stageGO = StageLibrary.Instance.Instantiate(stageKey, stageRoot);
        p1GO = CharacterLibrary.Instance.Instantiate(p1Key, p1Spawn.position, p1Spawn.rotation);
        p2GO = CharacterLibrary.Instance.Instantiate(p2Key, p2Spawn.position, p2Spawn.rotation);

        var p1Prop = p1GO.GetComponent<CharacterProperty>();
        var p2Prop = p2GO.GetComponent<CharacterProperty>();

        RegisterFighter(p1Prop, 1);
        RegisterFighter(p2Prop, 2);

        p1Prop?.SpawnAt(p1Spawn.position, true);
        p2Prop?.SpawnAt(p2Spawn.position, false);

        roundController.BindFighters(p1Prop, p2Prop);
        StartCoroutine(Co_BeginAfterReady());

        ConfigureInputSources(p1Prop, p2Prop);

        cameraRig.fighters[0] = p1GO.transform;
        cameraRig.fighters[1] = p2GO.transform;

        hpBar_p1.SetCharacter(p1Prop);
        hpBar_p2.SetCharacter(p2Prop);

        driveBar_p1.SetCharacter(p1Prop);
        driveBar_p2.SetCharacter(p2Prop);

        saBar_p1.SetCharacter(p1Prop);
        saBar_p2.SetCharacter(p2Prop);

        combos_p1.BindCharacter(p2Prop);
        combos_p2.BindCharacter(p1Prop);
        combos_p1.Init();
        combos_p2.Init();
    }

    private System.Collections.IEnumerator Co_BeginAfterReady()
    {
        yield return WaitFor.TickMasterReady();
        yield return WaitFor.PhysicsManagerReady();
        yield return WaitFor.BoxManagerReady();
        roundController.BeginMatch();
    }

    private void ConfigureInputSources(CharacterProperty p1Prop, CharacterProperty p2Prop)
    {
        var gm = GameManager.Instance;
        var cfg = gm.matchConfig;
        if (cfg == null) return;

        // 1) 각 캐릭터에서 InputSourceInstaller를 꺼냄
        var ins1 = p1Prop.GetComponent<InputSourceInstaller>();
        var ins2 = p2Prop.GetComponent<InputSourceInstaller>();

        // 없으면 아무 것도 하지 않음(최소 수정 원칙)
        if (ins1 == null || ins2 == null) return;

        // 2) 모드/로컬 플레이어 구분
        var mode = gm.currentMode;                          // GameMode.PvCPU / OnlinePvP / etc.
        var mySlot = gm.currentUser != null ? gm.currentUser.slotId : PlayerSlotId.P1; // 기본값 P1

        // 3) 각 슬롯의 로드아웃
        var p1 = cfg.p1;
        var p2 = cfg.p2;

        // 4) 네트워크 모드의 경우, 공유 NetProvider 준비(없으면 생성)
        if (mode == GameMode.OnlinePvP && _sharedNetProvider == null)
            _sharedNetProvider = new NetworkInputProvider();

        // 5) 슬롯별 컨트롤러 타입 결정 (최소규칙)
        ControllerType ToControllerType(PlayerLoadout L, PlayerSlotId slot)
        {
            switch (mode)
            {
                case GameMode.PvCPU:
                    // PvCPU: Human vs CPU 가정. (SetPlayer에서 isCpu 세팅됨)
                    return L.isCpu ? ControllerType.AI : ControllerType.Local;

                case GameMode.OnlinePvP:
                    // OnlinePvP: 내 슬롯은 Local, 상대 슬롯은 Network
                    return (slot == mySlot) ? ControllerType.Local : ControllerType.Network;

                case GameMode.Story:
                    return L.isCpu ? ControllerType.AI : ControllerType.Local;

                case GameMode.Training:
                    // 예: L.isDummy가 true면 Dummy, 아니면 Local(또는 AI)
                    if (L.isDummy) return ControllerType.Dummy;
                    return L.isCpu ? ControllerType.AI : ControllerType.Local;

                default:
                    // 스토리도 기본은 Local (필요 시 나중에 스크립트/AI로 대체)
                    return L.isCpu ? ControllerType.AI : ControllerType.Local;
            }
        }

        var p1Type = ToControllerType(p1, PlayerSlotId.P1);
        var p2Type = ToControllerType(p2, PlayerSlotId.P2);

        // 6) 실제 주입
        Apply(ins1, p1Type, p1, p1Prop);
        Apply(ins2, p2Type, p2, p2Prop);

        // --- 로컬 입력장치 활성화는 Installer 내부에서 Arbiter 사용으로 통일 ---
        // InputBuffer.captureFromDevice = false 로 이미 설정됨(Installer.Start).
        // LocalInputProvider는 GameManager.Instance.actions 를 읽어옴.
    }

    private void Apply(InputSourceInstaller ins, ControllerType type, PlayerLoadout L, CharacterProperty owner)
    {
        ins.character = owner;
        if (ins.inputBuffer == null) ins.inputBuffer = owner.GetComponent<InputBuffer>();

        ins.controllerType = type;

        switch (type)
        {
            case ControllerType.Local:
                // 별도 세팅 불필요. Installer가 GameManager.Instance.actions를 사용.
                ins.networkProviderExternal = null;
                break;

            case ControllerType.AI:
                // 필요시 캐릭터/코스튬/난이도에 맞는 매크로를 GameManager(or Catalog)에서 받아서 세팅
                // 최소 수정: zip에 있는 필드만 그대로 활용 (null이어도 동작은 함)
                // ex) ins.aiMacro = SomeMacroLibrary.Resolve(L.characterId);
                ins.networkProviderExternal = null;
                break;

            case ControllerType.Network:
                // 양쪽 중 네트워크 쪽에만 공유 Provider 주입
                ins.networkProviderExternal = _sharedNetProvider ?? ins.networkProviderExternal;
                break;

            case ControllerType.Replay:
                // 최소 수정: zip 기준으로 replaySource는 외부에서 세팅/주입하거나 null 허용
                // ins.replaySource = ...; ins.replayLoop = true/false;
                ins.networkProviderExternal = null;
                break;
        }

        // Installer.Start()가 생성/등록을 수행하므로,
        // 배틀 중간에 바꾼 경우엔 재설치를 원하면 인위적으로 다시 호출 필요.
        // 지금은 스폰 직후라 Start가 자연히 호출됨.
    }

}
