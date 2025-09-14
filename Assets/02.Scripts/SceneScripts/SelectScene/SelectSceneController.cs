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
        actions.Select.Enable();       // 셀렉트 맵만 켠다
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
    public int index;      // 현재 호버 인덱스
    public bool locked;    // 확정 여부
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
        // View 이벤트 구독
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

        // 최초 빌드: Model → View
        view.BuildCharacterGrid(model.Characters);
        view.BuildStageGrid(model.Stages);
        view.SetCharacterGridOn();

        // 커서 생성
        p1 = new SelectCursor { playerId = 0, index = 0, locked = false };
        p2 = new SelectCursor { playerId = 1, index = 1, locked = false };

        // 모드별 입력 소스 매핑
        var human = new HumanSelectorInput(actions);
        switch (gameMode)
        {
            case GameMode.Story:
                p1.input = human;
                p2.input = new NullInput(); // 혹은 Null
                break;

            case GameMode.PvCPU:
                p1.input = human;
                p2.input = new NullInput(); // 1단계에선 잠시 무시
                break;

            case GameMode.OnlinePvP:
                p1.input = human;
                p2.input = new NullInput(); // 네트워크 훅 연결
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

        // 입력 처리
        TickCursor(p1, count);
        TickCursor(p2, count);

        // 페이즈 전환 규칙
        if (gridMode == GridMode.Character)
        {
            if (gameMode == GameMode.Story && p1.locked) NextPhase();
            if (gameMode == GameMode.PvCPU)
            {
                if (!p1.locked) return;
                // 1단계 끝나면 2P를 P1 프록시로 전환 → 2P 선택 시작
                if (p2.input is NullInput) p2.input = new ProxySelectorInput(p1.input);
                if (p2.locked) NextPhase();
            }
            if (gameMode == GameMode.OnlinePvP && p1.locked && p2.locked) NextPhase();
        }
        else
        {
            // 스테이지는 보통 P1만 확정해도 OK
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
                    "0" // costume Id //TODO: 추후 누른 버튼에 따라, 동일캐릭터 선택 시 바뀌도록
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
                // 2P 입력 소스 프록시로 전환
                if (p2.input is NullInput) p2.input = new ProxySelectorInput(p1.input);

                // 2P 포커스 표시 켜기
                // (필요하면 바로 위치도 한번 보정)
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

        // 스테이지는 보통 P1만 조작
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
        // 방향성(dot) + 거리 역수(가까울수록 우선)로 가장 자연스러운 후보 선택
        int listCount = gridMode == GridMode.Character ? model.Characters.Count : model.Stages.Count;
        Vector2 cur = gridMode == GridMode.Character ? model.Characters[curIdx].gridPos : model.Stages[curIdx].gridPos;

        float best = float.NegativeInfinity; int bestIdx = curIdx;
        for (int i = 0; i < listCount; i++)
        {
            if (i == curIdx) continue;
            Vector2 tgt = gridMode == GridMode.Character ? model.Characters[i].gridPos : model.Stages[i].gridPos;
            Vector2 to = tgt - cur; if (to.sqrMagnitude < 1e-6f) continue;

            float dot = Vector2.Dot(to.normalized, dir.normalized); // 방향 일치도(코사인)
            if (dot < 0.35f) continue; // 70° 이상 벌어지면 제외

            float invDist = 1f / (to.magnitude + 0.001f); // 가까울수록 점수↑
            float score = dot * 0.8f + invDist * 0.2f;

            if (score > best) { best = score; bestIdx = i; }
        }
        return bestIdx;
    }

    void HandleCancel(SelectCursor c)
    {
        // 1) Stage 페이즈
        if (gridMode == GridMode.Stage)
        {
            if (c.locked)
            {
                // 스테이지만 해제
                c.locked = false;
                GameManager.Instance.ClearStage();
                RefreshFocusVisibility();
                return;
            }

            // 스테이지에서 캐릭터로 롤백
            RollbackToCharacterPhase();
            return;
        }

        // 2) Character 페이즈
        if (c.locked)
        {
            // 내 캐릭터 해제
            c.locked = false;
            GameManager.Instance.UnlockPlayer(c.playerId == 0 ? PlayerSlotId.P1 : PlayerSlotId.P2);

            // PvCPU: P1 해제 시 2P 프록시 조종 중단
            if (gameMode == GameMode.PvCPU && c.playerId == 0)
                p2.input = new NullInput();

            RefreshFocusVisibility();
            return;
        }

        // 3) 최상위(잠금 없음) → 이전 화면/타이틀/매치메이킹 취소
        ExitFromSelect();
        RefreshFocusVisibility();
    }

    // 캐릭터 페이즈로 롤백하는 공용 함수
    void RollbackToCharacterPhase()
    {
        gridMode = GridMode.Character;

        // 커서 잠금 상태는 초기화(캐릭터 재선택 허용)
        p1.locked = p2.locked = false;

        // 세션의 스테이지 선택 해제(안전)
        GameManager.Instance.ClearStage();

        // PvCPU: 캐릭터 다시 고를 땐 2P는 일단 비활성
        if (gameMode == GameMode.PvCPU)
            p2.input = new NullInput();

        // 뷰 전환
        view.SetCharacterGridOn();
        view.SetFocus(0, p1.index);
        view.SetFocus(1, p2.index);
        RefreshFocusVisibility();
    }

    // 씬 밖 정책은 한 곳으로 모음(타이틀 복귀/매치메이킹 취소 등)
    void ExitFromSelect()
    {
        switch (gameMode)
        {
            case GameMode.Story:
            case GameMode.PvCPU:
                // 예: 타이틀로
                UnityEngine.SceneManagement.SceneManager.LoadScene("Title");
                break;

            case GameMode.OnlinePvP:
                // 예: 매치메이킹 취소 확인창/콜백(임시로 타이틀 복귀)
                // ShowConfirm("Leave matchmaking?", onYes: () => { ... });
                UnityEngine.SceneManagement.SceneManager.LoadScene("Title");
                break;
        }
    }

    // ----- View 이벤트 처리 -----

    void OnViewReady()
    {
        // 필요 시 BGM/SFX, 툴팁, 타이머 등 시작
    }

    // 방향키 입력: (dir, playerId)
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
            if (data.isLocked) { /* 락 효과음/연출 */ return; }

            // TODO: GameSessionModel.Instance.SetPlayer(... data.addressableName ...)
            // 둘 다 확정되면 mode 전환 or StageSelect로 이동
        }
        else // Stage
        {
            int idx = playerId == 0 ? p1.index : p2.index;
            var data = model.Stages[idx];
            if (data.isLocked) return;

            // TODO: GameSessionModel.Instance.SetStage(data.addressableName)
            // TODO: 규칙 확인 후 Battle 씬으로 이동
        }
    }

    void OnCancel(int playerId)
    {
        // 선택 취소/이전 메뉴 로직
    }

    void OnRandom(int playerId)
    {
        // 랜덤 선택 로직 (Character/Stage 모드에 따라)
    }

    void OnHoverIndexChanged(int playerId, int idx)
    {
        // 툴팁/일러스트/프리뷰 업데이트 등
        // mode에 따라 model.Characters[idx] 또는 model.Stages[idx] 참조
    }

    // ----- 네비게이션(방향 → 다음 인덱스) 샘플 알고리즘 -----
    int ComputeNextIndex(Vector2 dir, int curIdx)
    {
        // 간단 버전: "해당 방향으로 가장 가까운 셀" (중점→중점 벡터로 판단)
        int count = gridMode == GridMode.Character ? model.Characters.Count : model.Stages.Count;
        if (count == 0) return 0;

        Vector2 curPos = (gridMode == GridMode.Character)
            ? model.Characters[curIdx].gridPos
            : model.Stages[curIdx].gridPos;

        float bestScore = float.NegativeInfinity;
        int bestIdx = curIdx;

        // 방향성(코사인)과 거리 가중치로 점수화
        for (int i = 0; i < count; i++)
        {
            if (i == curIdx) continue;
            Vector2 to = ((gridMode == GridMode.Character) ? model.Characters[i].gridPos : model.Stages[i].gridPos) - curPos;
            if (to.sqrMagnitude < 0.0001f) continue;

            Vector2 nTo = to.normalized;
            float dot = Vector2.Dot(nTo, dir.normalized);
            if (dot < 0.35f) continue; // 70도 이상 벌어진 건 후보 제외

            float invDist = 1.0f / (to.magnitude + 0.001f);
            float score = dot * 0.8f + invDist * 0.2f; // 방향 우선 + 거리 보정

            if (score > bestScore)
            {
                bestScore = score;
                bestIdx = i;
            }
        }

        // 후보가 없으면 제자리 유지
        return bestIdx;
    }

    // 모드 전환(캐릭터/스테이지) 필요 시 공개 메서드
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
        // 스테이지 모드에서도 p1Idx/p2Idx를 재사용하거나 초기화할지 선택
        view.SetFocus(0, p1.index);
        view.SetFocus(1, p2.index);
        RefreshFocusVisibility();
    }

    void RefreshFocusVisibility()
    {
        bool showP1 = true; // 항상 보이는 게 일반적

        bool showP2;
        if (gridMode == GridMode.Stage)
        {
            // 스테이지 선택은 P1만 조작 → P2 포커스 숨김
            showP2 = false;
        }
        else // Character
        {
            switch (gameMode)
            {
                case GameMode.Story:
                    showP2 = false; // 스토리는 1P만
                    break;

                case GameMode.PvCPU:
                    // 1) P1 캐릭 확정 전: P2는 비활성(숨김)
                    // 2) P1 확정 후: P1이 프록시로 2P 캐릭을 고르므로 P2 포커스 표시
                    showP2 = p1.locked;
                    break;

                case GameMode.OnlinePvP:
                    showP2 = true;   // 항상 양쪽 표시
                    break;

                default:
                    showP2 = false;
                    break;
            }
        }

        view.SetFocusVisible(showP1, showP2);
    }
}
