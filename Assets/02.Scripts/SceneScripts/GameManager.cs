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

    // ���� ���� ���
    public GameMode currentMode = GameMode.PvCPU;

    // �� ���� SessionSnapshot ��� �̰͸� ���
    public MatchConfig matchConfig;

    // �ֱ� ��ġ ��� (ResultScene ��)
    public BattleResult lastResult;

    // (����) �Է� �׼� �����ϰ� ������ ����
    public InputSystem_Actions actions { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        if (actions == null) actions = new InputSystem_Actions();
        if (matchConfig == null) matchConfig = new MatchConfig();
    }

    // ===== �� API =====
    public void SetMode(GameMode mode) => currentMode = mode;

    public void SetMatch(MatchConfig cfg) => matchConfig = cfg;

    public MatchConfig CurrentMatch => matchConfig;

    public void SetResult(BattleResult r) => lastResult = r;

    // Ÿ��Ʋ�� ���ư��� �����ϰ� �ʱ�ȭ�ϰ� ���� ��
    public void ResetForTitle()
    {
        matchConfig = null;
        lastResult = null;
        currentMode = GameMode.PvCPU; // �⺻�� �ƹ��ų�
    }

    // ====== ���Ž� ȣȯ API (SelectSceneController�� ȣ��) ======

    // ĳ����/�÷��̾� ����
    public void SetPlayer(PlayerSlotId slot, string characterAddressableName, PlayerType type, string costumeId)
    {
        EnsureMatchConfig();

        var loadout = slot == PlayerSlotId.P1 ? matchConfig.p1 : matchConfig.p2;
        loadout.characterId = characterAddressableName;
        loadout.costumeId = costumeId;
        loadout.isCpu = (type == PlayerType.CPU);
        // ����: ��Ʈ��ũ/�Է� ��Ŵ ����
        loadout.controlSchemeId =
            (type == PlayerType.Human) ? (slot == PlayerSlotId.P1 ? "P1" : "P2") :
            (type == PlayerType.Network) ? "Net" : null;

        if (slot == PlayerSlotId.P1) matchConfig.p1 = loadout; else matchConfig.p2 = loadout;
    }

    // �÷��̾� ���� ����
    public void UnlockPlayer(PlayerSlotId slot)
    {
        EnsureMatchConfig();
        var loadout = slot == PlayerSlotId.P1 ? matchConfig.p1 : matchConfig.p2;
        loadout.characterId = null;        // ���� ���
        loadout.isCpu = false;             // �⺻
        if (slot == PlayerSlotId.P1) matchConfig.p1 = loadout; else matchConfig.p2 = loadout;
    }

    // �������� ����/����
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

    // ��Ʋ ���� �غ� �Ϸ�?
    public bool IsReadyToStart()
    {
        EnsureMatchConfig();
        bool hasP1 = !string.IsNullOrEmpty(matchConfig.p1?.characterId);
        bool hasP2 =
            currentMode == GameMode.PvCPU
                ? !string.IsNullOrEmpty(matchConfig.p2?.characterId) || true // P2�� CPU ��� �� ĳ�� ���� ���̵� OK�� �η��� true
                : !string.IsNullOrEmpty(matchConfig.p2?.characterId);

        bool hasStage = !string.IsNullOrEmpty(matchConfig.stageId);
        return hasP1 && hasP2 && hasStage;
    }

    private void EnsureMatchConfig()
    {
        if (matchConfig == null) matchConfig = new MatchConfig();
        // ��� ��ũ(����)
        matchConfig.mode = currentMode;
    }

    protected void OnDisable()
    {
        actions?.Disable(); // ��� �� ����
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
