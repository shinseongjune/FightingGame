using System.Collections.Generic;
using UnityEngine;

public sealed class ReplayRecorder : MonoBehaviour
{
    public InputBuffer target; // �� ������ ������ �������� ��ȭ
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
        // ��ī��/����
        return new Dictionary<long, InputData>(record);
    }

    void FixedUpdate()
    {
        if (!isRecording || target == null) return;
        long tick = TickMaster.Instance != null ? TickMaster.Instance.CurrentTick : 0;
        // InputBuffer�� �� ƽ�� ���� Ȯ���� ������(LastInput)�� ��Ƶд�.
        var d = target.LastInput;
        d.tick = tick;
        record[tick] = d;
    }
}