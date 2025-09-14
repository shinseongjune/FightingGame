using UnityEngine;

public interface ISelectorInput
{
    Vector2 ReadMove();
    bool PressSubmit();
    bool PressCancel();
}

public sealed class HumanSelectorInput : ISelectorInput
{
    private readonly InputSystem_Actions actions;
    public HumanSelectorInput(InputSystem_Actions a)
    {
        actions = a;
        actions.Select.Enable();       // ����Ʈ �ʸ� �Ҵ�
    }

    public Vector2 ReadMove() => actions.Select.Navigate.ReadValue<Vector2>();
    public bool PressSubmit() => actions.Select.Submit.WasPressedThisFrame();
    public bool PressCancel() => actions.Select.Cancel.WasPressedThisFrame();
}

public sealed class ProxySelectorInput : ISelectorInput
{
    private readonly ISelectorInput source;
    public ProxySelectorInput(ISelectorInput src) { source = src; }
    public Vector2 ReadMove() => source.ReadMove();
    public bool PressSubmit() => source.PressSubmit();
    public bool PressCancel() => source.PressCancel();
}

public sealed class NullInput : ISelectorInput
{
    public Vector2 ReadMove() => Vector2.zero;
    public bool PressSubmit() => false;
    public bool PressCancel() => false;
}

public sealed class SelectCursor
{
    public int playerId;   // 0 = P1, 1 = P2
    public int index;      // ���� ȣ�� �ε���
    public bool locked;    // Ȯ�� ����
    public ISelectorInput input;
}

public class SelectSceneController : MonoBehaviour
{
    [SerializeField] private SelectSceneModel model;
    [SerializeField] private SelectSceneView view;

    private GameMode gameMode => GameManager.Instance.currentMode;
    private InputSystem_Actions actions => GameManager.Instance.actions;

    private enum GridMode { Character, Stage }
    private GridMode gridMode = GridMode.Character;
    
    private SelectCursor p1, p2;

    void Awake()
    {
        // View �̺�Ʈ ����
        view.OnViewReady += OnViewReady;
        view.OnNavigate += OnNavigate;
        view.OnSubmit += OnSubmit;
        view.OnCancel += OnCancel;
        view.OnRandom += OnRandom;
        view.OnHoverIndexChanged += OnHoverIndexChanged;
    }

    void Start()
    {
        if (model.Characters.Count <= 0 || model.Stages.Count <= 0) SceneLoader.Instance.LoadScene("TitleScene");

        // ���� ����: Model �� View
        view.BuildCharacterGrid(model.Characters);
        view.BuildStageGrid(model.Stages);
        view.SetCharacterGridOn();

        // Ŀ�� ����
        p1 = new SelectCursor { playerId = 0, index = 0, locked = false };
        p2 = new SelectCursor { playerId = 1, index = 1, locked = false };

        // ��庰 �Է� �ҽ� ����
        var human = new HumanSelectorInput(actions);
        switch (gameMode)
        {
            case GameMode.Story:
                p1.input = human;
                p2.input = new NullInput(); // Ȥ�� Null
                break;

            case GameMode.PvCPU:
                p1.input = human;
                p2.input = new NullInput(); // 1�ܰ迡�� ��� ����
                break;

            case GameMode.OnlinePvP:
                p1.input = human;
                p2.input = new NullInput(); // ��Ʈ��ũ �� ����
                break;
        }

        view.SetFocus(0, p1.index);
        //view.SetFocus(1, p2.index);
        view.InitDone();

        RefreshFocusVisibility();
    }

    void Update()
    {
        int count = gridMode == GridMode.Character ? model.Characters.Count : model.Stages.Count;

        // �Է� ó��
        TickCursor(p1, count);
        TickCursor(p2, count);

        // ������ ��ȯ ��Ģ
        if (gridMode == GridMode.Character)
        {
            if (gameMode == GameMode.Story && p1.locked) NextPhase();
            if (gameMode == GameMode.PvCPU)
            {
                if (!p1.locked) return;
                // 1�ܰ� ������ 2P�� P1 ���Ͻ÷� ��ȯ �� 2P ���� ����
                if (p2.input is NullInput) p2.input = new ProxySelectorInput(p1.input);
                if (p2.locked) NextPhase();
            }
            if (gameMode == GameMode.OnlinePvP && p1.locked && p2.locked) NextPhase();
        }
        else
        {
            // ���������� ���� P1�� Ȯ���ص� OK
            if (p1.locked) FinishAndStartBattle();
        }
    }

    void TickCursor(SelectCursor c, int count)
    {
        if (c.locked) return;

        var move = c.input.ReadMove();
        if (move.sqrMagnitude > 0.2f)
        {
            int next = ComputeNextIndex(move, c.index, count);
            if (next != c.index) { c.index = next; view.SetFocus(c.playerId, c.index); }
        }

        if (c.input.PressSubmit())
        {
            if (gridMode == GridMode.Character)
            {
                var data = model.Characters[c.index];
                if (data.isLocked) return;

                GameManager.Instance.SetPlayer(
                    c.playerId == 0 ? PlayerSlotId.P1 : PlayerSlotId.P2,
                    data.addressableName,
                    (gameMode == GameMode.PvCPU && c.playerId == 1) ? PlayerType.CPU : PlayerType.Human,
                    "0" // costume Id //TODO: ���� ���� ��ư�� ����, ����ĳ���� ���� �� �ٲ��
                );
            }
            else
            {
                var st = model.Stages[c.index];
                if (st.isLocked) return;
                GameManager.Instance.SetStage(st.addressableName);
            }
            c.locked = true;
            if (c.playerId == 0 && gameMode == GameMode.PvCPU)
            {
                // 2P �Է� �ҽ� ���Ͻ÷� ��ȯ
                if (p2.input is NullInput) p2.input = new ProxySelectorInput(p1.input);

                // 2P ��Ŀ�� ǥ�� �ѱ�
                // (�ʿ��ϸ� �ٷ� ��ġ�� �ѹ� ����)
                view.SetFocus(1, p2.index);
            }
            RefreshFocusVisibility();
        }

        if (c.input.PressCancel())
        {
            if (c.locked)
            {
                c.locked = false;
                GameManager.Instance.UnlockPlayer(c.playerId == 0 ? PlayerSlotId.P1 : PlayerSlotId.P2);
            }
            else
            {
                HandleCancel(c);
            }

            RefreshFocusVisibility();
        }
    }

    void NextPhase()
    {
        gridMode = GridMode.Stage;
        p1.locked = p2.locked = false;

        // ���������� ���� P1�� ����
        p2.input = new NullInput();

        view.SetStageGridOn();
        view.SetFocus(0, p1.index = 0);
        view.SetFocus(1, p2.index = 0);

        RefreshFocusVisibility();
    }

    void FinishAndStartBattle()
    {
        if (!GameManager.Instance.IsReadyToStart()) return;
        UnityEngine.SceneManagement.SceneManager.LoadScene("BattleScene");
    }

    int ComputeNextIndex(Vector2 dir, int curIdx, int count)
    {
        // ���⼺(dot) + �Ÿ� ����(�������� �켱)�� ���� �ڿ������� �ĺ� ����
        int listCount = gridMode == GridMode.Character ? model.Characters.Count : model.Stages.Count;
        Vector2 cur = gridMode == GridMode.Character ? model.Characters[curIdx].gridPos : model.Stages[curIdx].gridPos;

        float best = float.NegativeInfinity; int bestIdx = curIdx;
        for (int i = 0; i < listCount; i++)
        {
            if (i == curIdx) continue;
            Vector2 tgt = gridMode == GridMode.Character ? model.Characters[i].gridPos : model.Stages[i].gridPos;
            Vector2 to = tgt - cur; if (to.sqrMagnitude < 1e-6f) continue;

            float dot = Vector2.Dot(to.normalized, dir.normalized); // ���� ��ġ��(�ڻ���)
            if (dot < 0.35f) continue; // 70�� �̻� �������� ����

            float invDist = 1f / (to.magnitude + 0.001f); // �������� ������
            float score = dot * 0.8f + invDist * 0.2f;

            if (score > best) { best = score; bestIdx = i; }
        }
        return bestIdx;
    }

    void HandleCancel(SelectCursor c)
    {
        // 1) Stage ������
        if (gridMode == GridMode.Stage)
        {
            if (c.locked)
            {
                // ���������� ����
                c.locked = false;
                GameManager.Instance.ClearStage();
                RefreshFocusVisibility();
                return;
            }

            // ������������ ĳ���ͷ� �ѹ�
            RollbackToCharacterPhase();
            return;
        }

        // 2) Character ������
        if (c.locked)
        {
            // �� ĳ���� ����
            c.locked = false;
            GameManager.Instance.UnlockPlayer(c.playerId == 0 ? PlayerSlotId.P1 : PlayerSlotId.P2);

            // PvCPU: P1 ���� �� 2P ���Ͻ� ���� �ߴ�
            if (gameMode == GameMode.PvCPU && c.playerId == 0)
                p2.input = new NullInput();

            RefreshFocusVisibility();
            return;
        }

        // 3) �ֻ���(��� ����) �� ���� ȭ��/Ÿ��Ʋ/��ġ����ŷ ���
        ExitFromSelect();
        RefreshFocusVisibility();
    }

    // ĳ���� ������� �ѹ��ϴ� ���� �Լ�
    void RollbackToCharacterPhase()
    {
        gridMode = GridMode.Character;

        // Ŀ�� ��� ���´� �ʱ�ȭ(ĳ���� �缱�� ���)
        p1.locked = p2.locked = false;

        // ������ �������� ���� ����(����)
        GameManager.Instance.ClearStage();

        // PvCPU: ĳ���� �ٽ� �� �� 2P�� �ϴ� ��Ȱ��
        if (gameMode == GameMode.PvCPU)
            p2.input = new NullInput();

        // �� ��ȯ
        view.SetCharacterGridOn();
        view.SetFocus(0, p1.index);
        view.SetFocus(1, p2.index);
        RefreshFocusVisibility();
    }

    // �� �� ��å�� �� ������ ����(Ÿ��Ʋ ����/��ġ����ŷ ��� ��)
    void ExitFromSelect()
    {
        switch (gameMode)
        {
            case GameMode.Story:
            case GameMode.PvCPU:
                // ��: Ÿ��Ʋ��
                UnityEngine.SceneManagement.SceneManager.LoadScene("Title");
                break;

            case GameMode.OnlinePvP:
                // ��: ��ġ����ŷ ��� Ȯ��â/�ݹ�(�ӽ÷� Ÿ��Ʋ ����)
                // ShowConfirm("Leave matchmaking?", onYes: () => { ... });
                UnityEngine.SceneManagement.SceneManager.LoadScene("Title");
                break;
        }
    }

    // ----- View �̺�Ʈ ó�� -----

    void OnViewReady()
    {
        // �ʿ� �� BGM/SFX, ����, Ÿ�̸� �� ����
    }

    // ����Ű �Է�: (dir, playerId)
    void OnNavigate(Vector2 dir, int playerId)
    {
        int cur = playerId == 0 ? p1.index : p2.index;
        int next = ComputeNextIndex(dir, cur);
        if (playerId == 0) p1.index = next; else p2.index = next;
        view.SetFocus(playerId, next);
    }

    void OnSubmit(int playerId)
    {
        if (gridMode == GridMode.Character)
        {
            int idx = playerId == 0 ? p1.index : p2.index;
            var data = model.Characters[idx];
            if (data.isLocked) { /* �� ȿ����/���� */ return; }

            // TODO: GameSessionModel.Instance.SetPlayer(... data.addressableName ...)
            // �� �� Ȯ���Ǹ� mode ��ȯ or StageSelect�� �̵�
        }
        else // Stage
        {
            int idx = playerId == 0 ? p1.index : p2.index;
            var data = model.Stages[idx];
            if (data.isLocked) return;

            // TODO: GameSessionModel.Instance.SetStage(data.addressableName)
            // TODO: ��Ģ Ȯ�� �� Battle ������ �̵�
        }
    }

    void OnCancel(int playerId)
    {
        // ���� ���/���� �޴� ����
    }

    void OnRandom(int playerId)
    {
        // ���� ���� ���� (Character/Stage ��忡 ����)
    }

    void OnHoverIndexChanged(int playerId, int idx)
    {
        // ����/�Ϸ���Ʈ/������ ������Ʈ ��
        // mode�� ���� model.Characters[idx] �Ǵ� model.Stages[idx] ����
    }

    // ----- �׺���̼�(���� �� ���� �ε���) ���� �˰��� -----
    int ComputeNextIndex(Vector2 dir, int curIdx)
    {
        // ���� ����: "�ش� �������� ���� ����� ��" (���������� ���ͷ� �Ǵ�)
        int count = gridMode == GridMode.Character ? model.Characters.Count : model.Stages.Count;
        if (count == 0) return 0;

        Vector2 curPos = (gridMode == GridMode.Character)
            ? model.Characters[curIdx].gridPos
            : model.Stages[curIdx].gridPos;

        float bestScore = float.NegativeInfinity;
        int bestIdx = curIdx;

        // ���⼺(�ڻ���)�� �Ÿ� ����ġ�� ����ȭ
        for (int i = 0; i < count; i++)
        {
            if (i == curIdx) continue;
            Vector2 to = ((gridMode == GridMode.Character) ? model.Characters[i].gridPos : model.Stages[i].gridPos) - curPos;
            if (to.sqrMagnitude < 0.0001f) continue;

            Vector2 nTo = to.normalized;
            float dot = Vector2.Dot(nTo, dir.normalized);
            if (dot < 0.35f) continue; // 70�� �̻� ������ �� �ĺ� ����

            float invDist = 1.0f / (to.magnitude + 0.001f);
            float score = dot * 0.8f + invDist * 0.2f; // ���� �켱 + �Ÿ� ����

            if (score > bestScore)
            {
                bestScore = score;
                bestIdx = i;
            }
        }

        // �ĺ��� ������ ���ڸ� ����
        return bestIdx;
    }

    // ��� ��ȯ(ĳ����/��������) �ʿ� �� ���� �޼���
    public void SwitchToCharacter()
    {
        gridMode = GridMode.Character;
        view.SetCharacterGridOn();
        view.SetFocus(0, p1.index);
        view.SetFocus(1, p2.index);
        RefreshFocusVisibility();
    }

    public void SwitchToStage()
    {
        gridMode = GridMode.Stage;
        view.SetStageGridOn();
        // �������� ��忡���� p1Idx/p2Idx�� �����ϰų� �ʱ�ȭ���� ����
        view.SetFocus(0, p1.index);
        view.SetFocus(1, p2.index);
        RefreshFocusVisibility();
    }

    void RefreshFocusVisibility()
    {
        bool showP1 = true; // �׻� ���̴� �� �Ϲ���

        bool showP2;
        if (gridMode == GridMode.Stage)
        {
            // �������� ������ P1�� ���� �� P2 ��Ŀ�� ����
            showP2 = false;
        }
        else // Character
        {
            switch (gameMode)
            {
                case GameMode.Story:
                    showP2 = false; // ���丮�� 1P��
                    break;

                case GameMode.PvCPU:
                    // 1) P1 ĳ�� Ȯ�� ��: P2�� ��Ȱ��(����)
                    // 2) P1 Ȯ�� ��: P1�� ���Ͻ÷� 2P ĳ���� ���Ƿ� P2 ��Ŀ�� ǥ��
                    showP2 = p1.locked;
                    break;

                case GameMode.OnlinePvP:
                    showP2 = true;   // �׻� ���� ǥ��
                    break;

                default:
                    showP2 = false;
                    break;
            }
        }

        view.SetFocusVisible(showP1, showP2);
    }
}
