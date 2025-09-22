using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoundController : MonoBehaviour, ITicker
{
    private TickMaster _tm;

    public event Action<int> OnRoundStart;        // roundIndex (1..)
    public event Action<int, int> OnRoundEnd;      // (roundIndex, winnerSlot 1/2/0)
    public event Action<int> OnMatchEnd;          // winnerSlot 1/2/0

    // 외부에서 바인딩
    [SerializeField] MatchRules_SO rulesSO;

    CharacterProperty p1, p2;
    CharacterFSM f1, f2;
    PhysicsEntity ph1, ph2;
    InputBuffer in1, in2;

    enum Phase { None, PreRound, Fighting, PostRound, MatchEnd }
    Phase phase = Phase.None;

    // 진행 상태
    int roundsToWin;
    int roundIndex;
    int p1RoundsWon, p2RoundsWon;
    float remainTime;                     // 초 단위 표시용
    int preFreezeRemain;                  // 틱
    int postFreezeRemain;                 // 틱

    // 캐시
    MatchRulesData R;

    // UI 바인딩용(선택)
    public Action<float> OnTimerChanged;  // 남은 시간(초) 브로드캐스트
    public Action<int, int> OnRoundCountChanged; // (p1, p2)

    bool inited;

    // ====== 공개 API ======
    public void BindFighters(CharacterProperty p1Prop, CharacterProperty p2Prop)
    {
        p1 = p1Prop; p2 = p2Prop;
        f1 = p1?.GetComponent<CharacterFSM>();
        f2 = p2?.GetComponent<CharacterFSM>();
        ph1 = p1?.GetComponent<PhysicsEntity>();
        ph2 = p2?.GetComponent<PhysicsEntity>();
        in1 = p1?.GetComponent<InputBuffer>();
        in2 = p2?.GetComponent<InputBuffer>();
    }

    public void BeginMatch()
    {
        if (TickMaster.Instance == null || !TickMaster.Instance.IsReady)
        {
            StartCoroutine(Co_DeferBegin());
            return;
        }

        R = rulesSO != null ? rulesSO.data : MatchRulesData.Default;
        roundsToWin = Mathf.Max(1, R.winTarget);
        p1RoundsWon = p2RoundsWon = 0;
        roundIndex = 0;
        inited = true;

        TickMaster.Instance.Register(this);
        NextRound();
    }

    private System.Collections.IEnumerator Co_DeferBegin()
    {
        yield return WaitFor.TickMasterReady();
        BeginMatch();
    }

    void OnEnable()
    {
        _tm = TickMaster.Instance;

        if (inited)
            _tm?.Register(this);
    }
    void OnDisable()
    {
        _tm?.Unregister(this);
    }

    // ====== 라운드 루프 ======
    public void Tick()
    {
        if (!inited || phase == Phase.None) return;

        switch (phase)
        {
            case Phase.PreRound:
                // 타이머 멈춤
                if (preFreezeRemain > 0)
                {
                    preFreezeRemain--;
                    // 준비 연출 틱 동안은 아무 것도 하지 않음
                    if (preFreezeRemain <= 0)
                        StartFighting();
                }
                break;

            case Phase.Fighting:
                // 초 단위 감소 (TickMaster 60fps 기준)
                remainTime -= TickMaster.TICK_INTERVAL;
                if (remainTime < 0) remainTime = 0;
                OnTimerChanged?.Invoke(remainTime);

                // 승패 판단
                int winner = CheckWinConditionDuringFight();
                if (winner != int.MinValue) // -∞가 "아직"을 뜻하게 임의 지정
                {
                    EndRound(winner);
                    return;
                }
                break;

            case Phase.PostRound:
                if (postFreezeRemain > 0)
                {
                    postFreezeRemain--;
                    if (postFreezeRemain <= 0)
                    {
                        // 다음 라운드 또는 매치 종료
                        if (p1RoundsWon >= roundsToWin || p2RoundsWon >= roundsToWin)
                            EndMatch();
                        else
                            NextRound();
                    }
                }
                break;

            case Phase.MatchEnd:
                // Scene 전이는 EndMatch 내부에서 처리
                break;
        }
    }

    // ====== 전이 ======
    void NextRound()
    {
        roundIndex++;
        // 리셋
        ResetFightersForNewRound();

        // 준비 단계 진입
        phase = Phase.PreRound;
        remainTime = R.roundTimerSeconds;
        preFreezeRemain = Mathf.Max(0, R.preRoundFreezeTicks);

        // 입력/물리 잠금 + 준비 연출
        LockInputsAndPhysics(true);
        PlayRoundIntroIfAny();

        OnRoundStart?.Invoke(roundIndex);
        OnTimerChanged?.Invoke(remainTime);
        OnRoundCountChanged?.Invoke(p1RoundsWon, p2RoundsWon);
    }

    void StartFighting()
    {
        // 입력/물리 해제
        LockInputsAndPhysics(false);

        // 둘 다 뉴트럴로
        ForceIdleBoth();

        phase = Phase.Fighting;
    }

    void EndRound(int winnerSlot)
    {
        phase = Phase.PostRound;

        if (winnerSlot == 1) p1RoundsWon++;
        else if (winnerSlot == 2) p2RoundsWon++;
        // draw(0)은 승수 증가 없음

        OnRoundEnd?.Invoke(roundIndex, winnerSlot);
        OnRoundCountChanged?.Invoke(p1RoundsWon, p2RoundsWon);

        // 입력/물리 잠금 + KO/승리 연출
        LockInputsAndPhysics(true);
        PlayKoOrWinPose(winnerSlot);

        postFreezeRemain = Mathf.Max(0, R.koFreezeTicks);
    }

    void EndMatch()
    {
        phase = Phase.MatchEnd;
        LockInputsAndPhysics(true);

        int winner = (p1RoundsWon > p2RoundsWon) ? 1 :
                     (p2RoundsWon > p1RoundsWon) ? 2 : 0;

        OnMatchEnd?.Invoke(winner);

        // GameManager에 결과 저장 후 ResultScene 로드
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.lastResult = new BattleResult
            {
                winnerSlot = winner,
                p1Rounds = p1RoundsWon,
                p2Rounds = p2RoundsWon
            };
        }
        SceneManager.LoadScene("ResultScene");
    }

    // ====== 도우미 ======
    int CheckWinConditionDuringFight()
    {
        // 1) HP로 KO
        bool p1Dead = p1 != null && p1.hp <= 0;
        bool p2Dead = p2 != null && p2.hp <= 0;
        if (p1Dead && p2Dead) return 0;
        if (p1Dead) return 2;
        if (p2Dead) return 1;

        // 2) 타임업
        if (remainTime <= 0f)
        {
            if (p1.hp == p2.hp) return 0;
            return (p1.hp > p2.hp) ? 1 : 2;
        }

        // 3) 계속
        return int.MinValue;
    }

    void ResetFightersForNewRound()
    {
        if (p1 == null || p2 == null) return;

        // HP, 게이지 등
        p1.hp = p1.maxHp;
        p2.hp = p2.maxHp;
        p1.pendingHitstunFrames = p2.pendingHitstunFrames = 0;
        p1.pendingBlockstunFrames = p2.pendingBlockstunFrames = 0;

        // 위치/방향
        if (ph1 != null) { ph1.Position = R.p1Spawn; ph1.SyncTransform(); }
        if (ph2 != null) { ph2.Position = R.p2Spawn; ph2.SyncTransform(); }
        p1.SetFacing(R.p1FacingRight);
        p2.SetFacing(R.p2FacingRight);

        // 상태 초기화
        ForceIdleBoth();

        // 필요하면 박스/리졸버 초기화도 추가
        var bpa1 = p1.GetComponent<BoxPresetApplier>(); bpa1?.ClearAllBoxes();
        var bpa2 = p2.GetComponent<BoxPresetApplier>(); bpa2?.ClearAllBoxes();
    }

    void ForceIdleBoth()
    {
        f1?.ForceSetState("Idle");
        f2?.ForceSetState("Idle");
    }

    void LockInputsAndPhysics(bool locked)
    {
        if (in1 != null) in1.captureFromDevice = !locked;
        if (in2 != null) in2.captureFromDevice = !locked;

        if (ph1 != null)
        {
            ph1.mode = locked ? PhysicsMode.Kinematic : PhysicsMode.Normal;
            ph1.isGravityOn = !locked;
            ph1.Velocity = Vector2.zero;
        }
        if (ph2 != null)
        {
            ph2.mode = locked ? PhysicsMode.Kinematic : PhysicsMode.Normal;
            ph2.isGravityOn = !locked;
            ph2.Velocity = Vector2.zero;
        }
    }

    void PlayRoundIntroIfAny()
    {
        TryForceClip(f1, AnimKey.PreBattle, fallbackClipKey: null);
        TryForceClip(f2, AnimKey.PreBattle, fallbackClipKey: null);
    }

    void PlayKoOrWinPose(int winnerSlot)
    {
        if (winnerSlot == 1)
        {
            TryForceClip(f1, AnimKey.Win, null);
            TryForceClip(f2, AnimKey.Lose, null);
        }
        else if (winnerSlot == 2)
        {
            TryForceClip(f2, AnimKey.Win, null);
            TryForceClip(f1, AnimKey.Lose, null);
        }
        else // 무승부
        {
            TryForceClip(f1, AnimKey.Lose, null);
            TryForceClip(f2, AnimKey.Lose, null);
        }
    }

    // FSM에 강제 1회 재생 요청
    void TryForceClip(CharacterFSM fsm, AnimKey key, string fallbackClipKey)
    {
        if (fsm == null) return;
        var animCfg = fsm.GetComponent<CharacterAnimationConfig>();
        if (animCfg == null) return;
        
        string clipKey = animCfg.animSet != null ? animCfg.animSet.GetOrDefault(key, fallbackClipKey) : fallbackClipKey;
        if (string.IsNullOrEmpty(clipKey))
        {
            // 강제 상태만 켜고 Idle 포즈 유지 (연출 스킵)
            fsm.ForceSetState("Idle");
            return;
        }

        // ForcedAnimationState가 clipKey를 받아서 1회 재생할 수 있어야 함
        var forced = fsm.GetComponent<ForcedAnimationStateBridge>();
        
        if (forced) forced.PlayOnce(clipKey);
    }
}
