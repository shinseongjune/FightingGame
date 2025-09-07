// BattleManager.cs
using System.Collections;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private Transform p1Spawn;
    [SerializeField] private Transform p2Spawn;

    [Header("Modules (pluggable)")]
    [SerializeField] private MonoBehaviour stageLoaderBehaviour;   // IStageLoader
    [SerializeField] private MonoBehaviour characterFactoryBehaviour; // ICharacterFactory
    [SerializeField] private RoundController roundController;      // 모듈식 라운드 컨트롤러

    private IStageLoader stageLoader;
    private ICharacterFactory characterFactory;

    private CharacterProperty p1, p2;

    private void Awake()
    {
        stageLoader = stageLoaderBehaviour as IStageLoader;
        characterFactory = characterFactoryBehaviour as ICharacterFactory;

        if (stageLoader == null) Debug.LogError("[Battle] IStageLoader가 필요합니다.");
        if (characterFactory == null) Debug.LogError("[Battle] ICharacterFactory가 필요합니다.");
        if (roundController == null) Debug.LogError("[Battle] RoundController 참조가 필요합니다.");
    }

    private void OnEnable()
    {
        if (roundController != null)
        {
            roundController.OnRoundStart += HandleRoundStart;
            roundController.OnRoundEnd += HandleRoundEnd;
            roundController.OnMatchEnd += HandleMatchEnd;
        }
    }

    private void OnDisable()
    {
        if (roundController != null)
        {
            roundController.OnRoundStart -= HandleRoundStart;
            roundController.OnRoundEnd -= HandleRoundEnd;
            roundController.OnMatchEnd -= HandleMatchEnd;
        }
    }

    private void Start()
    {
        StartCoroutine(BootstrapFlow());
    }

    private IEnumerator BootstrapFlow()
    {
        // 1) 매치 구성 얻기
        var gm = GameManager.Instance;
        var cfg = gm != null ? gm.CurrentMatch : null;
        if (cfg == null)
        {
            Debug.LogWarning("[Battle] MatchConfig가 없습니다. 타이틀로 복귀합니다.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("TitleScene");
            yield break;
        }

        // 2) 스테이지 로드
        if (stageLoader != null)
        {
            yield return stageLoader.LoadAsync(cfg.stageId);
        }

        // 3) 캐릭터 스폰
        if (characterFactory != null)
        {
            var p1SpawnT = p1Spawn != null ? p1Spawn : transform;
            var p2SpawnT = p2Spawn != null ? p2Spawn : transform;

            var p1Task = characterFactory.SpawnAsync(cfg.p1, p1SpawnT.position, true);
            var p2Task = characterFactory.SpawnAsync(cfg.p2, p2SpawnT.position, false);

            // 병렬처럼 보이지만 코루틴 순차 실행. 필요하면 UniTask 등으로 병렬화.
            yield return p1Task;
            p1 = p1Task.Result;
            yield return p2Task;
            p2 = p2Task.Result;

            if (p1 == null || p2 == null)
            {
                Debug.LogError("[Battle] 캐릭터 스폰 실패");
                yield break;
            }
        }

        // 4) RoundController 초기화 및 라운드 시작
        roundController.Init(
            roundsToWin: cfg.roundsToWin,
            roundTimerSec: cfg.roundTimerSec,
            p1: p1,
            p2: p2);

        roundController.StartFirstRound();
    }

    // ----- 라운드 이벤트 처리 -----
    private void HandleRoundStart(int roundIndex)
    {
        // 예: 카메라 리셋/인트로 연출/입력락 등
        // 필요시 HUD 초기화/타이머 표시 시작
    }

    private void HandleRoundEnd(int roundIndex, int winnerSlot /*1 or 2 or 0:Draw*/)
    {
        // 예: 승리 포즈/라운드 스코어 반영
    }

    private void HandleMatchEnd(int winnerSlot /*1 or 2 or 0:Draw*/)
    {
        // 결과 구성 후 ResultScene으로
        var result = new BattleResult
        {
            winnerSlot = winnerSlot,
            p1Rounds = roundController.P1RoundsWon,
            p2Rounds = roundController.P2RoundsWon,
        };

        GameManager.Instance.SetResult(result);
        UnityEngine.SceneManagement.SceneManager.LoadScene("ResultScene");
    }
}
