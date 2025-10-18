using System.Collections.Generic;

public sealed class NetworkInputProvider : IInputProvider
{
    private readonly Dictionary<long, InputData> received = new();
    private InputData lastSnapshot;

    public bool enableSimplePrediction = true;

    // 네트워크 레이어에서 호출: 원격 입력을 밀어넣음
    public void OnRemoteInput(long tick, InputData d)
    {
        d.tick = tick; // 신뢰할 수 있는 서버틱/송신틱 사용
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
            // 현재 틱 데이터가 없으면 직전 입력 유지(보수적 예측)
            lastSnapshot.tick = curr;
            return lastSnapshot;
        }

        // 예측 비활성화면 무입력
        return new InputData { tick = curr, direction = Direction.Neutral, attack = AttackKey.None };
    }
}