using System;
using System.Collections.Generic;
using UnityEngine;

public class SelectSceneController : MonoBehaviour
{
    [SerializeField] private SelectSceneModel model;
    [SerializeField] private SelectSceneView view;

    // � ���� ���� �ִ°�? (ĳ����/��������)
    private enum GridMode { Character, Stage }
    [SerializeField] private GridMode mode = GridMode.Character;

    // �� �÷��̾� Ŀ�� �ε��� ����
    private int p1Idx = 0;
    private int p2Idx = 1;

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
        // ���� ����: Model �� View
        view.BuildCharacterGrid(model.Characters);
        view.BuildStageGrid(model.Stages);
        view.SetCharacterGridOn();
        view.SetFocus(0, p1Idx);
        view.SetFocus(1, p2Idx);
        view.InitDone();
    }

    // ----- View �̺�Ʈ ó�� -----

    void OnViewReady()
    {
        // �ʿ� �� BGM/SFX, ����, Ÿ�̸� �� ����
    }

    // ����Ű �Է�: (dir, playerId)
    void OnNavigate(Vector2 dir, int playerId)
    {
        int cur = playerId == 0 ? p1Idx : p2Idx;
        int next = ComputeNextIndex(dir, cur);
        if (playerId == 0) p1Idx = next; else p2Idx = next;
        view.SetFocus(playerId, next);
    }

    void OnSubmit(int playerId)
    {
        if (mode == GridMode.Character)
        {
            int idx = playerId == 0 ? p1Idx : p2Idx;
            var data = model.Characters[idx];
            if (data.isLocked) { /* �� ȿ����/���� */ return; }

            // TODO: GameSessionModel.Instance.SetPlayer(... data.addressableName ...)
            // �� �� Ȯ���Ǹ� mode ��ȯ or StageSelect�� �̵�
        }
        else // Stage
        {
            int idx = playerId == 0 ? p1Idx : p2Idx;
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
        int count = mode == GridMode.Character ? model.Characters.Count : model.Stages.Count;
        if (count == 0) return 0;

        Vector2 curPos = (mode == GridMode.Character)
            ? model.Characters[curIdx].gridPos
            : model.Stages[curIdx].gridPos;

        float bestScore = float.NegativeInfinity;
        int bestIdx = curIdx;

        // ���⼺(�ڻ���)�� �Ÿ� ����ġ�� ����ȭ
        for (int i = 0; i < count; i++)
        {
            if (i == curIdx) continue;
            Vector2 to = ((mode == GridMode.Character) ? model.Characters[i].gridPos : model.Stages[i].gridPos) - curPos;
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
        mode = GridMode.Character;
        view.SetCharacterGridOn();
        view.SetFocus(0, p1Idx);
        view.SetFocus(1, p2Idx);
    }

    public void SwitchToStage()
    {
        mode = GridMode.Stage;
        view.SetStageGridOn();
        // �������� ��忡���� p1Idx/p2Idx�� �����ϰų� �ʱ�ȭ���� ����
        view.SetFocus(0, p1Idx);
        view.SetFocus(1, p2Idx);
    }
}
