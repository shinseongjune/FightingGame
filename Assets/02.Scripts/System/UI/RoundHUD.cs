using UnityEngine;

public class RoundHUD : MonoBehaviour
{
    public GameManager gm;
    public RoundController rc;
    public CharacterProperty p1, p2;

    void OnGUI()
    {
        if (!gm || !rc) return;

        // ���� ���ε�: p1/p2�� ��� ������ GM���� ������
        if (p1 == null && gm.p1 != null) p1 = gm.p1;
        if (p2 == null && gm.p2 != null) p2 = gm.p2;

        if (p1 == null || p2 == null) { GUILayout.Label("Waiting for spawn..."); return; }

        GUILayout.BeginArea(new Rect(10, 10, 320, 120), GUI.skin.box);
        GUILayout.Label($"Timer: {rc.RemainSeconds:00}");
        GUILayout.Label($"P1 HP: {(int)p1.hp}/{(int)p1.maxHp}   Wins: {gm.p1Wins}");
        GUILayout.Label($"P2 HP: {(int)p2.hp}/{(int)p2.maxHp}   Wins: {gm.p2Wins}");
        GUILayout.EndArea();
    }
}
