using System;
using UnityEngine;

public class RoundController : MonoBehaviour, ITicker
{
    public event Action<int> OnRoundStart;                                   // roundIndex(1..)
    public event Action<int, int> OnRoundEnd;                                // (roundIndex, winnerSlot 1/2/0)
    public event Action<int> OnMatchEnd;                                     // winnerSlot 1/2/0

    public int P1RoundsWon { get; private set; }
    public int P2RoundsWon { get; private set; }

    int roundsToWin;
    int roundTimerSec;
    int roundIndex;
    float remainTime;

    CharacterProperty p1, p2;

    bool inRound;

    public void Init(int roundsToWin, int roundTimerSec, CharacterProperty p1, CharacterProperty p2)
    {
        this.roundsToWin = Mathf.Max(1, roundsToWin);
        this.roundTimerSec = Mathf.Max(1, roundTimerSec);
        this.p1 = p1;
        this.p2 = p2;

        P1RoundsWon = 0;
        P2RoundsWon = 0;
        roundIndex = 0;

        TickMaster.Instance?.Register(this);
    }

    public void StartFirstRound() => StartNextRound();

    public void Tick()
    {
        if (!inRound) return;

        remainTime -= TickMaster.TICK_INTERVAL;
        if (remainTime <= 0f)
        {
            EndRound(DetermineWinnerByHpOrDraw());
        }

        // KO 체크 (hp는 CharacterProperty에 존재)
        if (p1.hp <= 0 || p2.hp <= 0)
        {
            int winner = (p1.hp <= 0 && p2.hp <= 0) ? 0 : (p1.hp <= 0 ? 2 : 1);
            EndRound(winner);
        }
    }

    void StartNextRound()
    {
        roundIndex++;
        remainTime = roundTimerSec;
        inRound = true;

        // 라운드 시작 시 캐릭터 리셋(필요한 만큼만)
        ResetFightersForNewRound();

        OnRoundStart?.Invoke(roundIndex);
    }

    void EndRound(int winnerSlot)
    {
        if (!inRound) return;
        inRound = false;

        if (winnerSlot == 1) P1RoundsWon++;
        else if (winnerSlot == 2) P2RoundsWon++;

        OnRoundEnd?.Invoke(roundIndex, winnerSlot);

        // 매치 종료 판정
        if (P1RoundsWon >= roundsToWin || P2RoundsWon >= roundsToWin)
        {
            int matchWinner = P1RoundsWon > P2RoundsWon ? 1 : (P2RoundsWon > P1RoundsWon ? 2 : 0);
            OnMatchEnd?.Invoke(matchWinner);
            TickMaster.Instance?.Unregister(this);
            return;
        }

        // 다음 라운드로
        StartNextRound();
    }

    int DetermineWinnerByHpOrDraw()
    {
        if (Mathf.Approximately(p1.hp, p2.hp)) return 0;
        return (p1.hp > p2.hp) ? 1 : 2;
    }

    void ResetFightersForNewRound()
    {
        // HP/게이지/위치/방향 리셋. (필요한 규칙만 우선)
        p1.hp = p1.maxHp; p2.hp = p2.maxHp;

        // 위치는 BattleManager의 스폰 지점이 더 정확하지만, 간단 초기화 예시:
        var p1Phys = p1.GetComponent<PhysicsEntity>();
        var p2Phys = p2.GetComponent<PhysicsEntity>();
        if (p1Phys != null) { p1Phys.Position = new Vector2(-3, 0); p1Phys.SyncTransform(); }
        if (p2Phys != null) { p2Phys.Position = new Vector2(+3, 0); p2Phys.SyncTransform(); }
        p1.SetFacing(true);
        p2.SetFacing(false);

        // 상태 초기화(필요 시 FSM 강제 전이)
        var f1 = p1.GetComponent<CharacterFSM>(); f1?.ForceSetState("Idle");
        var f2 = p2.GetComponent<CharacterFSM>(); f2?.ForceSetState("Idle");
    }
}
