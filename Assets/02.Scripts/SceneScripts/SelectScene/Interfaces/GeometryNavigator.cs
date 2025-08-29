using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ��(������ �ݴ볡 �̵�) ��å
/// </summary>
public enum WrapPolicy { None, AxisCycle /*�� ���� ����Ŭ*/ }

/// <summary>
/// ���⼺/�Ÿ�/���/������ �����ؼ� ���� ��Ŀ���� �����ϴ� �׺������.
/// - View�� SetSnapshot���� ������ �ѱ��.
/// - TryNavigate�� ���� id�� �Է� ���ͷ� ���� id�� �����ش�.
/// </summary>
public sealed class GeometryNavigator
{
    // Ʃ�� �Ķ����(������Ʈ �Ծ࿡ ���� SO ������ ���� ��)
    public float forwardEpsilon = 0.0001f; // ���� ���� �Ӱ�
    public float bandWidth = 160f;         // ���� ��/���� �����ϴ� ����(����) ��� ����(px)
    public float weightForward = 1.0f;     // ���� �Ÿ� ����ġ(�������� ������ �켱)
    public float weightSide = 0.65f;       // ���� ���� ����ġ
    public float weightAngle = 0.35f;      // ���� ����ġ(���� �Ǵ� ���� �ϰ��ǰ� ���)
    public float lockedPenalty = 1e6f;     // ���/����/��Ȱ�� �ĺ� �о��

    public WrapPolicy wrap = WrapPolicy.AxisCycle;

    // ������
    private List<NodeGeom> _nodes = new();
    // id -> index ����(���� ��ȸ)
    private readonly Dictionary<int, int> _id2index = new();

    /// <summary> View�� ���̾ƿ��� ��ģ ������ ȣ��. </summary>
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
    /// ���� ��Ŀ�� id�� �Է� ���� ���ͷ� ���� ��Ŀ�� id�� ����.
    /// �Է� ���ʹ� (1,0),(-1,0),(0,1),(0,-1) �Ǵ� �Ƴ��α�(����ȭ ����).
    /// </summary>
    public bool TryNavigate(int currentId, Vector2 inputDir, out int nextId)
    {
        nextId = currentId;
        if (_nodes == null || _nodes.Count == 0) return false;
        if (!_id2index.TryGetValue(currentId, out int curIdx)) return false;

        // �Է� ����ȭ & ���� ����
        Vector2 v = inputDir.sqrMagnitude < 1e-9f ? Vector2.zero : inputDir.normalized;
        if (v == Vector2.zero) return false;
        Vector2 vPerp = new Vector2(-v.y, v.x);

        var p0 = _nodes[curIdx].center;

        // 1) ���� �ĺ� ����(���/����/��Ȱ�� ����)
        var candidates = new List<(int idx, float score)>(_nodes.Count);
        for (int i = 0; i < _nodes.Count; i++)
        {
            if (i == curIdx) continue;
            var n = _nodes[i];

            // ǥ��/��ȣ�ۿ� �����Ѱ�?
            if (n.hidden || !n.interactable) continue;

            Vector2 d = n.center - p0;
            float forward = Vector2.Dot(d, v);            // ���� ����(+�� ����)
            if (forward <= forwardEpsilon) continue;      // ��/�� ��κ� ��

            float side = Mathf.Abs(Vector2.Dot(d, vPerp)); // ���� ����
            // 2) ��� �켱(���� ��/�� ��ȣ)
            //   - ���� bandWidth �̳��� ���ϰ� �켱�ϰ�, �Ѵ� �ĺ��� ���ܼ� �־��� ��� �̵� ���� ����
            float bandPenalty = side <= bandWidth ? 0f : (side - bandWidth) * 0.25f;

            // 3) ����(���鿡 �������� ���ʽ�)
            float angle = AngleBetween01(v, d); // 0..1 ����ȭ ����(0=����,1=���ݴ�)

            // 4) ���� ���ھ�(�������� ����)
            float score =
                weightForward * forward
              + weightSide * side
              + weightAngle * angle * 100f   // ������ ����(�ȼ� ��� ���� ���� ���̰�)
              + bandPenalty;

            // ����� �ĺ��� ������ ����(Ȥ�� ū ���Ƽ�� �ڷ� �б� ���� ����)
            if (n.locked) score += lockedPenalty;

            // ���� �ĺ��� ���(�ּ�ȭ ������ ������� forward�� '��������'�� ���� ���
            // ���⼱ forward�� �״�� ���� ���߿� tie-break���� ����)
            candidates.Add((i, score));
        }

        int bestIdx = -1;

        if (candidates.Count > 0)
        {
            // 5) ���ھ� �ּڰ� ���� (tie-break: ���� �Ÿ� �� ª�� ��)
            candidates.Sort((a, b) =>
            {
                int c = a.score.CompareTo(b.score);
                if (c != 0) return c;

                // �����̸� ���� �Ÿ�(�� ����) ���� �� �켱
                float fa = Vector2.Dot(_nodes[a.idx].center - p0, v);
                float fb = Vector2.Dot(_nodes[b.idx].center - p0, v);
                c = fa.CompareTo(fb);
                if (c != 0) return c;

                // ���� �����̸� ��Ŭ���� �Ÿ�
                float da = Vector2.SqrMagnitude(_nodes[a.idx].center - p0);
                float db = Vector2.SqrMagnitude(_nodes[b.idx].center - p0);
                return da.CompareTo(db);
            });

            bestIdx = candidates[0].idx;
        }
        else
        {
            // 6) ���� �ĺ��� ���� ��: �� ��å
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
    /// �� ���� + ����Ŭ ��:
    /// - �¿� �Է��̸� x��, ���� �Է��̸� y��(�Ǵ� �Է� ���� ��ü�� ��)
    /// - ���� �������� �������� ����/���� �ĺ��� ã�� ������ �ݴ볡���� ��
    /// - ������ ���/����/��Ȱ���� ����
    /// </summary>
    private int WrapAxisCycle(int curIdx, Vector2 v)
    {
        bool horizontal = Mathf.Abs(v.x) >= Mathf.Abs(v.y);
        // ���� ��: v ��ü�� �ᵵ �ǰ�, �������� �ᵵ ������
        // '���� ����' ������ ���� v�� ���(�ϰ��� ���)
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

        // ��/���������� ����/���� ã��
        if (forwardDir)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i].u > u0) return list[i].idx;
            // �� ã���� ��: ���� ó��
            return list[0].idx;
        }
        else
        {
            for (int i = list.Count - 1; i >= 0; i--)
                if (list[i].u < u0) return list[i].idx;
            // �� ã���� ��: ���� ��
            return list[^1].idx;
        }
    }

    /// <summary>
    /// v�� d ������ ������ 0..1�� ����ȭ(0=���� ����, 1=���ݴ�).
    /// atan2 ��� dot ������� ������ ó��.
    /// </summary>
    private static float AngleBetween01(Vector2 v, Vector2 d)
    {
        if (d.sqrMagnitude < 1e-9f) return 1f;
        float cos = Vector2.Dot(v, d.normalized);
        // cos �� [-1,1] �� 0..1�� ��
        // (1=����)��0, (-1=���ݴ�)��1 �� ġȯ�Ϸ���:
        return (1f - Mathf.Clamp01((cos + 1f) * 0.5f));
    }
}