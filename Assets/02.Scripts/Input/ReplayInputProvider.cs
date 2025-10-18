using System.Collections.Generic;

public sealed class ReplayInputProvider : IInputProvider
{
    // tick -> InputData
    private readonly Dictionary<long, InputData> timeline = new();
    private bool loop;
    private long beginTick = long.MaxValue;
    private long endTick = long.MinValue;

    public ReplayInputProvider(Dictionary<long, InputData> source, bool loop)
    {
        if (source != null)
        {
            foreach (var kv in source)
            {
                var d = kv.Value; d.tick = kv.Key;
                timeline[kv.Key] = d;
                beginTick = System.Math.Min(beginTick, kv.Key);
                endTick = System.Math.Max(endTick, kv.Key);
            }
        }
        this.loop = loop;
        if (beginTick == long.MaxValue) { beginTick = 0; endTick = 0; }
    }

    public InputData GetSnapshot()
    {
        long curr = TickMaster.Instance != null ? TickMaster.Instance.CurrentTick : 0;
        long t = curr;

        if (loop && endTick > beginTick)
        {
            long len = endTick - beginTick + 1;
            long offset = (curr - beginTick) % len;
            if (offset < 0) offset += len;
            t = beginTick + offset;
        }

        if (timeline.TryGetValue(t, out var d))
        {
            d.tick = curr; // ���� ƽ���� ������ �ٲ㵵 �ǰ� �� ƽ�� �����ص� ����(�Һ��� ���迡 ����)
            return d;
        }

        return new InputData { tick = curr, direction = Direction.Neutral, attack = AttackKey.None };
    }
}