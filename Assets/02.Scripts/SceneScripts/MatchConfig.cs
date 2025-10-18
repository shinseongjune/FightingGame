using System;
using UnityEngine;

[Serializable]
public class PlayerLoadout
{
    public int playerId;          // 1 or 2
    public string characterId;    // ĳ���� Ű(������/īŻ�α� id)
    public string costumeId;      // ����: �ڽ�Ƭ Ű
    public string controlSchemeId;// ����: �Է� ��Ŵ Ű
    public bool isCpu;            // PvCPU ��� ���
    public bool isDummy;          // ���� �÷��̾��
}

[Serializable]
public class MatchConfig
{
    public GameMode mode;
    public string stageId;        // �������� Ű(��/������ �ĺ���)

    public PlayerLoadout p1 = new PlayerLoadout { playerId = 1 };
    public PlayerLoadout p2 = new PlayerLoadout { playerId = 2 };

    public int roundsToWin = 2;   // 2����
    public int roundTimerSec = 99;// ���� Ÿ�̸�(��)
}

[Serializable]
public class BattleResult
{
    public int winnerSlot; // 1/2/0(draw)
    public int p1Rounds;
    public int p2Rounds;
}
