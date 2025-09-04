using System;
using System.Collections.Generic;
using UnityEngine;

public class SelectSceneController : MonoBehaviour
{
    [SerializeField] private SelectSceneModel model;
    [SerializeField] private SelectSceneView view;

    // 어떤 탭을 보고 있는가? (캐릭터/스테이지)
    private enum GridMode { Character, Stage }
    [SerializeField] private GridMode mode = GridMode.Character;

    // 두 플레이어 커서 인덱스 예시
    private int p1Idx = 0;
    private int p2Idx = 1;

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
        // 최초 빌드: Model → View
        view.BuildCharacterGrid(model.Characters);
        view.BuildStageGrid(model.Stages);
        view.SetCharacterGridOn();
        view.SetFocus(0, p1Idx);
        view.SetFocus(1, p2Idx);
        view.InitDone();
    }

    // ----- View 이벤트 처리 -----

    void OnViewReady()
    {
        // 필요 시 BGM/SFX, 툴팁, 타이머 등 시작
    }

    // 방향키 입력: (dir, playerId)
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
            if (data.isLocked) { /* 락 효과음/연출 */ return; }

            // TODO: GameSessionModel.Instance.SetPlayer(... data.addressableName ...)
            // 둘 다 확정되면 mode 전환 or StageSelect로 이동
        }
        else // Stage
        {
            int idx = playerId == 0 ? p1Idx : p2Idx;
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
        int count = mode == GridMode.Character ? model.Characters.Count : model.Stages.Count;
        if (count == 0) return 0;

        Vector2 curPos = (mode == GridMode.Character)
            ? model.Characters[curIdx].gridPos
            : model.Stages[curIdx].gridPos;

        float bestScore = float.NegativeInfinity;
        int bestIdx = curIdx;

        // 방향성(코사인)과 거리 가중치로 점수화
        for (int i = 0; i < count; i++)
        {
            if (i == curIdx) continue;
            Vector2 to = ((mode == GridMode.Character) ? model.Characters[i].gridPos : model.Stages[i].gridPos) - curPos;
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
        mode = GridMode.Character;
        view.SetCharacterGridOn();
        view.SetFocus(0, p1Idx);
        view.SetFocus(1, p2Idx);
    }

    public void SwitchToStage()
    {
        mode = GridMode.Stage;
        view.SetStageGridOn();
        // 스테이지 모드에서도 p1Idx/p2Idx를 재사용하거나 초기화할지 선택
        view.SetFocus(0, p1Idx);
        view.SetFocus(1, p2Idx);
    }
}
