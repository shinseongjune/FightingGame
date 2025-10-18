using System.Collections.Generic;

public sealed class NetworkInputProvider : IInputProvider
{
    private readonly Dictionary<long, InputData> received = new();
    private InputData lastSnapshot;

    public bool enableSimplePrediction = true;

    // ��Ʈ��ũ ���̾�� ȣ��: ���� �Է��� �о����
    public void OnRemoteInput(long tick, InputData d)
    {
        d.tick = tick; // �ŷ��� �� �ִ� ����ƽ/�۽�ƽ ���
        received[tick] = d;
    }

    public InputData GetSnapshot()
    {
        long curr = TickMaster.Instance != null ? TickMaster.Instance.CurrentTick : 0;

        if (received.TryGetValue(curr, out var d))
        {
            lastSnapshot = d;
            return d;
        }

        if (enableSimplePrediction)
        {
            // ���� ƽ �����Ͱ� ������ ���� �Է� ����(������ ����)
            lastSnapshot.tick = curr;
            return lastSnapshot;
        }

        // ���� ��Ȱ��ȭ�� ���Է�
        return new InputData { tick = curr, direction = Direction.Neutral, attack = AttackKey.None };
    }
}