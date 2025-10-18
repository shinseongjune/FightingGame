using System;
using UnityEngine;

[Serializable]
public class PlayerLoadout
{
    public int playerId;          // 1 or 2
    public string characterId;    // 캐릭터 키(프리팹/카탈로그 id)
    public string costumeId;      // 선택: 코스튬 키
    public string controlSchemeId;// 선택: 입력 스킴 키
    public bool isCpu;            // PvCPU 등에서 사용
    public bool isDummy;          // 더미 플레이어용
}

[Serializable]
public class MatchConfig
{
    public GameMode mode;
    public string stageId;        // 스테이지 키(씬/프리팹 식별자)

    public PlayerLoadout p1 = new PlayerLoadout { playerId = 1 };
    public PlayerLoadout p2 = new PlayerLoadout { playerId = 2 };

    public int roundsToWin = 2;   // 2선승
    public int roundTimerSec = 99;// 라운드 타이머(초)
}

[Serializable]
public class BattleResult
{
    public int winnerSlot; // 1/2/0(draw)
    public int p1Rounds;
    public int p2Rounds;
}
