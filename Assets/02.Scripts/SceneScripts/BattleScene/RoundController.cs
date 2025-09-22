using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoundController : MonoBehaviour, ITicker
{
    private TickMaster _tm;

    public event Action<int> OnRoundStart;        // roundIndex (1..)
    public event Action<int, int> OnRoundEnd;      // (roundIndex, winnerSlot 1/2/0)
    public event Action<int> OnMatchEnd;          // winnerSlot 1/2/0

    // �ܺο��� ���ε�
    [SerializeField] MatchRules_SO rulesSO;

    CharacterProperty p1, p2;
    CharacterFSM f1, f2;
    PhysicsEntity ph1, ph2;
    InputBuffer in1, in2;

    enum Phase { None, PreRound, Fighting, PostRound, MatchEnd }
    Phase phase = Phase.None;

    // ���� ����
    int roundsToWin;
    int roundIndex;
    int p1RoundsWon, p2RoundsWon;
    float remainTime;                     // �� ���� ǥ�ÿ�
    int preFreezeRemain;                  // ƽ
    int postFreezeRemain;                 // ƽ

    // ĳ��
    MatchRulesData R;

    // UI ���ε���(����)
    public Action<float> OnTimerChanged;  // ���� �ð�(��) ��ε�ĳ��Ʈ
    public Action<int, int> OnRoundCountChanged; // (p1, p2)

    bool inited;

    // ====== ���� API ======
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

    // ====== ���� ���� ======
    public void Tick()
    {
        if (!inited || phase == Phase.None) return;

        switch (phase)
        {
            case Phase.PreRound:
                // Ÿ�̸� ����
                if (preFreezeRemain > 0)
                {
                    preFreezeRemain--;
                    // �غ� ���� ƽ ������ �ƹ� �͵� ���� ����
                    if (preFreezeRemain <= 0)
                        StartFighting();
                }
                break;

            case Phase.Fighting:
                // �� ���� ���� (TickMaster 60fps ����)
                remainTime -= TickMaster.TICK_INTERVAL;
                if (remainTime < 0) remainTime = 0;
                OnTimerChanged?.Invoke(remainTime);

                // ���� �Ǵ�
                int winner = CheckWinConditionDuringFight();
                if (winner != int.MinValue) // -�İ� "����"�� ���ϰ� ���� ����
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
                        // ���� ���� �Ǵ� ��ġ ����
                        if (p1RoundsWon >= roundsToWin || p2RoundsWon >= roundsToWin)
                            EndMatch();
                        else
                            NextRound();
                    }
                }
                break;

            case Phase.MatchEnd:
                // Scene ���̴� EndMatch ���ο��� ó��
                break;
        }
    }

    // ====== ���� ======
    void NextRound()
    {
        roundIndex++;
        // ����
        ResetFightersForNewRound();

        // �غ� �ܰ� ����
        phase = Phase.PreRound;
        remainTime = R.roundTimerSeconds;
        preFreezeRemain = Mathf.Max(0, R.preRoundFreezeTicks);

        // �Է�/���� ��� + �غ� ����
        LockInputsAndPhysics(true);
        PlayRoundIntroIfAny();

        OnRoundStart?.Invoke(roundIndex);
        OnTimerChanged?.Invoke(remainTime);
        OnRoundCountChanged?.Invoke(p1RoundsWon, p2RoundsWon);
    }

    void StartFighting()
    {
        // �Է�/���� ����
        LockInputsAndPhysics(false);

        // �� �� ��Ʈ����
        ForceIdleBoth();

        phase = Phase.Fighting;
    }

    void EndRound(int winnerSlot)
    {
        phase = Phase.PostRound;

        if (winnerSlot == 1) p1RoundsWon++;
        else if (winnerSlot == 2) p2RoundsWon++;
        // draw(0)�� �¼� ���� ����

        OnRoundEnd?.Invoke(roundIndex, winnerSlot);
        OnRoundCountChanged?.Invoke(p1RoundsWon, p2RoundsWon);

        // �Է�/���� ��� + KO/�¸� ����
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

        // GameManager�� ��� ���� �� ResultScene �ε�
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

    // ====== ����� ======
    int CheckWinConditionDuringFight()
    {
        // 1) HP�� KO
        bool p1Dead = p1 != null && p1.hp <= 0;
        bool p2Dead = p2 != null && p2.hp <= 0;
        if (p1Dead && p2Dead) return 0;
        if (p1Dead) return 2;
        if (p2Dead) return 1;

        // 2) Ÿ�Ӿ�
        if (remainTime <= 0f)
        {
            if (p1.hp == p2.hp) return 0;
            return (p1.hp > p2.hp) ? 1 : 2;
        }

        // 3) ���
        return int.MinValue;
    }

    void ResetFightersForNewRound()
    {
        if (p1 == null || p2 == null) return;

        // HP, ������ ��
        p1.hp = p1.maxHp;
        p2.hp = p2.maxHp;
        p1.pendingHitstunFrames = p2.pendingHitstunFrames = 0;
        p1.pendingBlockstunFrames = p2.pendingBlockstunFrames = 0;

        // ��ġ/����
        if (ph1 != null) { ph1.Position = R.p1Spawn; ph1.SyncTransform(); }
        if (ph2 != null) { ph2.Position = R.p2Spawn; ph2.SyncTransform(); }
        p1.SetFacing(R.p1FacingRight);
        p2.SetFacing(R.p2FacingRight);

        // ���� �ʱ�ȭ
        ForceIdleBoth();

        // �ʿ��ϸ� �ڽ�/������ �ʱ�ȭ�� �߰�
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
        else // ���º�
        {
            TryForceClip(f1, AnimKey.Lose, null);
            TryForceClip(f2, AnimKey.Lose, null);
        }
    }

    // FSM�� ���� 1ȸ ��� ��û
    void TryForceClip(CharacterFSM fsm, AnimKey key, string fallbackClipKey)
    {
        if (fsm == null) return;
        var animCfg = fsm.GetComponent<CharacterAnimationConfig>();
        if (animCfg == null) return;
        
        string clipKey = animCfg.animSet != null ? animCfg.animSet.GetOrDefault(key, fallbackClipKey) : fallbackClipKey;
        if (string.IsNullOrEmpty(clipKey))
        {
            // ���� ���¸� �Ѱ� Idle ���� ���� (���� ��ŵ)
            fsm.ForceSetState("Idle");
            return;
        }

        // ForcedAnimationState�� clipKey�� �޾Ƽ� 1ȸ ����� �� �־�� ��
        var forced = fsm.GetComponent<ForcedAnimationStateBridge>();
        
        if (forced) forced.PlayOnce(clipKey);
    }
}
