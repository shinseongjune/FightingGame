using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 랩(끝에서 반대끝 이동) 정책
/// </summary>
public enum WrapPolicy { None, AxisCycle /*축 투영 사이클*/ }

/// <summary>
/// 방향성/거리/밴드/각도를 종합해서 다음 포커스를 결정하는 네비게이터.
/// - View는 SetSnapshot으로 노드들을 넘긴다.
/// - TryNavigate가 현재 id와 입력 벡터로 다음 id를 돌려준다.
/// </summary>
public sealed class GeometryNavigator
{
    // 튜닝 파라미터(프로젝트 규약에 따라 SO 등으로 빼도 됨)
    public float forwardEpsilon = 0.0001f; // 전방 판정 임계
    public float bandWidth = 160f;         // 같은 행/열로 간주하는 수직(측면) 허용 편차(px)
    public float weightForward = 1.0f;     // 전방 거리 가중치(작을수록 가깝게 우선)
    public float weightSide = 0.65f;       // 측면 편차 가중치
    public float weightAngle = 0.35f;      // 각도 가중치(라디안 또는 도를 일관되게 사용)
    public float lockedPenalty = 1e6f;     // 잠금/히든/비활성 후보 밀어내기

    public WrapPolicy wrap = WrapPolicy.AxisCycle;

    // 스냅샷
    private List<NodeGeom> _nodes = new();
    // id -> index 매핑(빠른 조회)
    private readonly Dictionary<int, int> _id2index = new();

    /// <summary> View가 레이아웃을 마친 시점에 호출. </summary>
    public void SetSnapshot(IReadOnlyList<NodeGeom> nodes)
    {
        _nodes = new List<NodeGeom>(nodes.Count);
        _id2index.Clear();
        for (int i = 0; i < nodes.Count; i++)
        {
            var n = nodes[i];
            _nodes.Add(n);
            _id2index[n.id] = i;
        }
    }

    /// <summary>
    /// 현재 포커스 id와 입력 방향 벡터로 다음 포커스 id를 결정.
    /// 입력 벡터는 (1,0),(-1,0),(0,1),(0,-1) 또는 아날로그(정규화 권장).
    /// </summary>
    public bool TryNavigate(int currentId, Vector2 inputDir, out int nextId)
    {
        nextId = currentId;
        if (_nodes == null || _nodes.Count == 0) return false;
        if (!_id2index.TryGetValue(currentId, out int curIdx)) return false;

        // 입력 정규화 & 수직 벡터
        Vector2 v = inputDir.sqrMagnitude < 1e-9f ? Vector2.zero : inputDir.normalized;
        if (v == Vector2.zero) return false;
        Vector2 vPerp = new Vector2(-v.y, v.x);

        var p0 = _nodes[curIdx].center;

        // 1) 전방 후보 수집(잠금/히든/비활성 제외)
        var candidates = new List<(int idx, float score)>(_nodes.Count);
        for (int i = 0; i < _nodes.Count; i++)
        {
            if (i == curIdx) continue;
            var n = _nodes[i];

            // 표시/상호작용 가능한가?
            if (n.hidden || !n.interactable) continue;

            Vector2 d = n.center - p0;
            float forward = Vector2.Dot(d, v);            // 전방 성분(+면 전방)
            if (forward <= forwardEpsilon) continue;      // 뒤/옆 대부분 컷

            float side = Mathf.Abs(Vector2.Dot(d, vPerp)); // 측면 편차
            // 2) 밴드 우선(같은 행/열 선호)
            //   - 먼저 bandWidth 이내를 강하게 우선하고, 넘는 후보도 남겨서 최악의 경우 이동 막힘 방지
            float bandPenalty = side <= bandWidth ? 0f : (side - bandWidth) * 0.25f;

            // 3) 각도(정면에 가까울수록 보너스)
            float angle = AngleBetween01(v, d); // 0..1 정규화 각도(0=정면,1=정반대)

            // 4) 종합 스코어(낮을수록 좋음)
            float score =
                weightForward * forward
              + weightSide * side
              + weightAngle * angle * 100f   // 스케일 보정(픽셀 대비 각도 영향 보이게)
              + bandPenalty;

            // 잠금은 후보에 남기지 않음(혹은 큰 페널티로 뒤로 밀기 선택 가능)
            if (n.locked) score += lockedPenalty;

            // 전방 후보에 등록(최소화 문제로 만들려면 forward를 '작을수록'로 쓰는 대신
            // 여기선 forward를 그대로 쓰고 나중에 tie-break에서 보정)
            candidates.Add((i, score));
        }

        int bestIdx = -1;

        if (candidates.Count > 0)
        {
            // 5) 스코어 최솟값 선택 (tie-break: 전방 거리 더 짧은 것)
            candidates.Sort((a, b) =>
            {
                int c = a.score.CompareTo(b.score);
                if (c != 0) return c;

                // 동점이면 전방 거리(축 투영) 작은 쪽 우선
                float fa = Vector2.Dot(_nodes[a.idx].center - p0, v);
                float fb = Vector2.Dot(_nodes[b.idx].center - p0, v);
                c = fa.CompareTo(fb);
                if (c != 0) return c;

                // 완전 동점이면 유클리드 거리
                float da = Vector2.SqrMagnitude(_nodes[a.idx].center - p0);
                float db = Vector2.SqrMagnitude(_nodes[b.idx].center - p0);
                return da.CompareTo(db);
            });

            bestIdx = candidates[0].idx;
        }
        else
        {
            // 6) 전방 후보가 없을 때: 랩 정책
            if (wrap == WrapPolicy.AxisCycle)
            {
                bestIdx = WrapAxisCycle(curIdx, v);
            }
        }

        if (bestIdx >= 0)
        {
            nextId = _nodes[bestIdx].id;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 축 투영 + 사이클 랩:
    /// - 좌우 입력이면 x축, 상하 입력이면 y축(또는 입력 벡터 자체의 축)
    /// - 현재 투영값을 기준으로 다음/이전 후보를 찾고 없으면 반대끝으로 랩
    /// - 여전히 잠금/히든/비활성은 제외
    /// </summary>
    private int WrapAxisCycle(int curIdx, Vector2 v)
    {
        bool horizontal = Mathf.Abs(v.x) >= Mathf.Abs(v.y);
        // 투영 축: v 자체를 써도 되고, 직교축을 써도 되지만
        // '진행 방향' 정렬을 위해 v를 사용(일관된 결과)
        Vector2 axis = v.normalized;

        var list = new List<(int idx, float u)>(_nodes.Count);
        for (int i = 0; i < _nodes.Count; i++)
        {
            var n = _nodes[i];
            if (i == curIdx) continue;
            if (n.hidden || !n.interactable || n.locked) continue;
            float u = Vector2.Dot(n.center, axis);
            list.Add((i, u));
        }
        if (list.Count == 0) return -1;

        list.Sort((a, b) => a.u.CompareTo(b.u));

        float u0 = Vector2.Dot(_nodes[curIdx].center, axis);
        bool forwardDir = horizontal ? (v.x > 0f) : (v.y > 0f);

        // 정/역방향으로 다음/이전 찾기
        if (forwardDir)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i].u > u0) return list[i].idx;
            // 못 찾으면 랩: 가장 처음
            return list[0].idx;
        }
        else
        {
            for (int i = list.Count - 1; i >= 0; i--)
                if (list[i].u < u0) return list[i].idx;
            // 못 찾으면 랩: 가장 끝
            return list[^1].idx;
        }
    }

    /// <summary>
    /// v와 d 사이의 각도를 0..1로 정규화(0=완전 정면, 1=정반대).
    /// atan2 대신 dot 기반으로 빠르게 처리.
    /// </summary>
    private static float AngleBetween01(Vector2 v, Vector2 d)
    {
        if (d.sqrMagnitude < 1e-9f) return 1f;
        float cos = Vector2.Dot(v, d.normalized);
        // cos ∈ [-1,1] → 0..1로 맵
        // (1=정면)→0, (-1=정반대)→1 로 치환하려면:
        return (1f - Mathf.Clamp01((cos + 1f) * 0.5f));
    }
}