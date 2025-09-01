using System.Collections.Generic;
using UnityEngine;

public struct GridNode
{
    public int id;
    public Vector2 pos;
}

public class GridNavigator
{ 
    // 튜닝 패러미터
    private float forwardEpsilon = 0.0001f;         // 전방 판정 임계
    private float bandWidth = 160f;                 // 같은 행/열로 간주하는 허용 편차
    private float weightForward = 1.0f;             // 전방 거리 가중치
    private float weightSide = 0.65f;               // 측면 편차 가중치
    private float lockedPenalty = 1e6f;             // 비활성 후보 밀어내기

    // 스냅샷
    List<GridNode> _nodes = new List<GridNode>();

    // <summary> View가 레이아웃을 마친 시점에 호출 </summary>
    void SetSnapShot(IReadOnlyList<GridNode> nodes)
    {

    }

    // <summary> 현재 포커스 id와 입력 방향 벡터로 다음 포커스 id를 결정 </summary>
    bool TryNavigate(int currentId, Vector2 inputDir, out int nextId)
    {
        nextId = currentId;
        return false;
    }

    // <summary> 축 투영 + 사이클 랩:
    // 좌우 입력이면 x축, 상하 입력이면 y축
    // 현재 투영값을 기준으로 다음/이전 후보를 찾고 없으면 반대 끝으로 랩
    // 비활성은 제외 </summary>
    int WrapAxisCycle(int currentId, Vector2 inputDir)
    {
        return currentId;
    }
}
