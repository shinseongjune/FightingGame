using System;
using UnityEngine;

public enum GameMode
{
    Story,
    PvCPU,
    OnlinePvP,
}

public enum PlayerSlotId { P1 = 0, P2 = 1 }
public enum PlayerType { Human, CPU }
public enum RoundFormat { FT1 = 1, FT2 = 2, FT3 = 3 }   // First-To
public enum TimerMode { Infinite = -1, Sec_99 = 99, Sec_60 = 60, Sec_30 = 30 }

public class GameManager : Singleton<GameManager>
{
    public GameMode gameMode = GameMode.PvCPU;

    public InputSystem_Actions actions { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        actions = new InputSystem_Actions();

        EnableSelectMap();
    }

    public void EnableSelectMap()
    {
        actions.Player.Disable();
        actions.Select.Enable();
    }

    public void EnablePlayerMap()
    {
        actions.Select.Disable();
        actions.Player.Enable();
    }

    [Serializable]
    public class SessionSnapshot
    {
        [Serializable]
        public class PlayerConfig
        {
            public PlayerType type = PlayerType.Human;
            public string characterKey;
            public bool lockedIn;
        }

        [Serializable]
        public class StageConfig
        {
            public string stageKey;
            public bool isRandom;
        }

        [Serializable]
        public class RuleConfig
        {
            public RoundFormat round = RoundFormat.FT2;
            public TimerMode timer = TimerMode.Sec_99;
            public bool allowMirrorMatch = true;
        }

        public PlayerConfig p1 = new();
        public PlayerConfig p2 = new();
        public StageConfig stage = new();
        public RuleConfig rules = new();
    }

    public SessionSnapshot Session { get; private set; } = new();

    public event Action OnSessionChanged;
    public event Action OnBothLockedIn;

    public void ResetSession()
    {
        Session = new SessionSnapshot();
        OnSessionChanged?.Invoke();
    }

    public void SetPlayer(PlayerSlotId slot, string characterKey, PlayerType type)
    {
        var p = slot == PlayerSlotId.P1 ? Session.p1 : Session.p2;
        p.characterKey = characterKey;
        p.type = type;
        p.lockedIn = true;
        OnSessionChanged?.Invoke();

        if (Session.p1.lockedIn && Session.p2.lockedIn)
            OnBothLockedIn?.Invoke();
    }

    public void UnlockPlayer(PlayerSlotId slot)
    {
        var p = slot == PlayerSlotId.P1 ? Session.p1 : Session.p2;
        p.lockedIn = false;
        OnSessionChanged?.Invoke();
    }

    public void SetStage(string stageKey, bool random = false)
    {
        Session.stage.stageKey = stageKey;
        Session.stage.isRandom = random;
        OnSessionChanged?.Invoke();
    }

    public void SetRules(RoundFormat round, TimerMode timer, bool allowMirror = true)
    {
        Session.rules.round = round;
        Session.rules.timer = timer;
        Session.rules.allowMirrorMatch = allowMirror;
        OnSessionChanged?.Invoke();
    }

    public bool IsReadyToStart()
    {
        if (!Session.p1.lockedIn || !Session.p2.lockedIn) return false;
        if (!Session.rules.allowMirrorMatch &&
            !string.IsNullOrEmpty(Session.p1.characterKey) &&
            Session.p1.characterKey == Session.p2.characterKey)
            return false;

        if (string.IsNullOrEmpty(Session.stage.stageKey) && !Session.stage.isRandom)
            return false;

        return true;
    }

    public void ClearStage()
    {
        Session.stage.stageKey = null;
        Session.stage.isRandom = false;
        OnSessionChanged?.Invoke();
    }

    public void UnlockBothPlayers()
    {
        Session.p1.lockedIn = false;
        Session.p2.lockedIn = false;
        OnSessionChanged?.Invoke();
    }
}
