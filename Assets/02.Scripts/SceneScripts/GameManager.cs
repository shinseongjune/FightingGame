public class UserData
{
    public string userName;
    public string password;
    public PlayerSlotId slotId;
}

public enum GameMode
{
    Story,
    PvCPU,
    OnlinePvP,
    Training,
}

public enum PlayerSlotId
{
    P1,
    P2,
}

public enum PlayerType
{
    Human,
    CPU,
    Network,
}

public class GameManager : Singleton<GameManager>
{
    public UserData currentUser;

    // 현재 게임 모드
    public GameMode currentMode = GameMode.PvCPU;

    // ★ 기존 SessionSnapshot 대신 이것만 사용
    public MatchConfig matchConfig;

    // 최근 매치 결과 (ResultScene 용)
    public BattleResult lastResult;

    // (선택) 입력 액션 공유하고 있으면 유지
    public InputSystem_Actions actions { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        if (actions == null) actions = new InputSystem_Actions();
        if (matchConfig == null) matchConfig = new MatchConfig();
    }

    // ===== 새 API =====
    public void SetMode(GameMode mode) => currentMode = mode;

    public void SetMatch(MatchConfig cfg) => matchConfig = cfg;

    public MatchConfig CurrentMatch => matchConfig;

    public void SetResult(BattleResult r) => lastResult = r;

    // 타이틀로 돌아가며 깨끗하게 초기화하고 싶을 때
    public void ResetForTitle()
    {
        matchConfig = null;
        lastResult = null;
        currentMode = GameMode.PvCPU; // 기본값 아무거나
    }

    // ====== 레거시 호환 API (SelectSceneController가 호출) ======

    // 캐릭터/플레이어 설정
    public void SetPlayer(PlayerSlotId slot, string characterAddressableName, PlayerType type, string costumeId)
    {
        EnsureMatchConfig();

        var loadout = slot == PlayerSlotId.P1 ? matchConfig.p1 : matchConfig.p2;
        loadout.characterId = characterAddressableName;
        loadout.costumeId = costumeId;
        loadout.isCpu = (type == PlayerType.CPU);
        // 선택: 네트워크/입력 스킴 매핑
        loadout.controlSchemeId =
            (type == PlayerType.Human) ? (slot == PlayerSlotId.P1 ? "P1" : "P2") :
            (type == PlayerType.Network) ? "Net" : null;

        if (slot == PlayerSlotId.P1) matchConfig.p1 = loadout; else matchConfig.p2 = loadout;
    }

    // 플레이어 선택 해제
    public void UnlockPlayer(PlayerSlotId slot)
    {
        EnsureMatchConfig();
        var loadout = slot == PlayerSlotId.P1 ? matchConfig.p1 : matchConfig.p2;
        loadout.characterId = null;        // 선택 취소
        loadout.isCpu = false;             // 기본
        if (slot == PlayerSlotId.P1) matchConfig.p1 = loadout; else matchConfig.p2 = loadout;
    }

    // 스테이지 설정/해제
    public void SetStage(string stageAddressableName)
    {
        EnsureMatchConfig();
        matchConfig.stageId = stageAddressableName;
    }

    public void ClearStage()
    {
        EnsureMatchConfig();
        matchConfig.stageId = null;
    }

    // 배틀 시작 준비 완료?
    public bool IsReadyToStart()
    {
        EnsureMatchConfig();
        bool hasP1 = !string.IsNullOrEmpty(matchConfig.p1?.characterId);
        bool hasP2 =
            currentMode == GameMode.PvCPU
                ? !string.IsNullOrEmpty(matchConfig.p2?.characterId) || true // P2는 CPU 허용 → 캐릭 지정 없이도 OK로 두려면 true
                : !string.IsNullOrEmpty(matchConfig.p2?.characterId);

        bool hasStage = !string.IsNullOrEmpty(matchConfig.stageId);
        return hasP1 && hasP2 && hasStage;
    }

    private void EnsureMatchConfig()
    {
        if (matchConfig == null) matchConfig = new MatchConfig();
        // 모드 싱크(선택)
        matchConfig.mode = currentMode;
    }

    protected void OnDisable()
    {
        actions?.Disable(); // 모든 맵 끄기
    }

    protected void OnDestroy()
    {
        actions?.Disable();
        actions?.Dispose();
        actions = null;
    }

    protected override void OnApplicationQuit()
    {
        base.OnApplicationQuit();

        actions?.Disable();
    }
}
