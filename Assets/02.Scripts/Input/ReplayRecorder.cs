using System.Collections.Generic;
using UnityEngine;

public sealed class ReplayRecorder : MonoBehaviour
{
    public InputBuffer target; // 이 버퍼의 마지막 스냅샷을 녹화
    public bool isRecording { get; private set; }

    private readonly Dictionary<long, InputData> record = new();

    public void StartRecording()
    {
        record.Clear();
        isRecording = true;
    }

    public Dictionary<long, InputData> StopRecording()
    {
        isRecording = false;
        // 딥카피/리턴
        return new Dictionary<long, InputData>(record);
    }

    void FixedUpdate()
    {
        if (!isRecording || target == null) return;
        long tick = TickMaster.Instance != null ? TickMaster.Instance.CurrentTick : 0;
        // InputBuffer가 그 틱에 최종 확정한 스냅샷(LastInput)을 잡아둔다.
        var d = target.LastInput;
        d.tick = tick;
        record[tick] = d;
    }
}