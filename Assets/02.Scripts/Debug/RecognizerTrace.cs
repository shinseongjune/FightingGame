using System;
using System.Collections.Generic;
using UnityEngine;

public static class RecognizerTrace
{
    static int _lastToggleFrame = -1;
    static bool _suppressAutoFreezeThisFrame = false;

    [Serializable]
    public class Attempt
    {
        public string skillName;
        public bool success;
        public string failReason;
        public int attackIdx = -1;
        public List<int> matchedIdx;
        public int gapsUsed;
        public int maxGap;
        public int sameFrameDirsAllowed;
    }

    // �� ������ ������
    [Serializable]
    public class FrameLog
    {
        public int frame;
        public float time;
        public InputData[] buffer;
        public List<Attempt> attempts;
    }

    // ���̺�(���� ������) ����
    public static InputData[] lastBuffer = Array.Empty<InputData>();
    public static readonly List<Attempt> attempts = new();

    // �� �����丮(������)
    static readonly List<FrameLog> history = new();
    public static int HistoryCapacity = 180;          // �ֱ� 3��(60fps ����)
    static FrameLog building;                          // ���� ������ ���� ��

    // �� ���� ����
    public static bool Frozen { get; private set; } = false;
    public static int ViewIndex { get; private set; } = -1; // -1=���̺�, �� ��: history �ε���

    // �� �ڵ� ������ �ɼ�
    public static bool AutoFreezeOnFail = true;
    public static int AutoFreezeHoldFrames = 30;       // ���� �� 0.5�� ����(60fps ����)
    static int freezeHoldCounter = 0;

    // �� ���� �佺Ʈ ����
    public static float SuccessToastDuration = 2.0f; // �� �� ��������
    public static int SuccessToastMax = 6;           // ȭ�鿡 ���ÿ� ������ �ִ� ����

    // �� �佺Ʈ ������
    public struct SuccessToast
    {
        public string text;
        public float t0Unscaled; // Time.unscaledTime ����
    }
    public static readonly List<SuccessToast> successToasts = new();

    // �� �佺Ʈ �߰� �Լ�
    public static void PushSuccessToast(string text)
    {
        successToasts.Add(new SuccessToast { text = text, t0Unscaled = Time.unscaledTime });
        // �ʹ� �������� ������ �ͺ��� ����
        while (successToasts.Count > SuccessToastMax) successToasts.RemoveAt(0);
    }

    // ������ ����: ���̺� ���� ��� + ���� ����
    public static void BeginFrame(InputData[] bufferSnapshot)
    {
        if (Frozen) return; // �� ���� �߿� ���̺� ������/�õ� �ʱ�ȭ ����
        lastBuffer = bufferSnapshot;
        attempts.Clear();
        building = new FrameLog
        {
            frame = Time.frameCount,
            time = Time.time,
            buffer = (InputData[])bufferSnapshot.Clone(),
            attempts = new List<Attempt>()
        };
    }

    // �õ� ����/��ŷ
    public static Attempt BeginAttempt(string skillName, int maxGap, int sameFrameDirsAllowed = 1)
    {
        var a = new Attempt
        {
            skillName = skillName,
            success = false,
            failReason = "",
            matchedIdx = new List<int>(),
            gapsUsed = 0,
            maxGap = maxGap,
            sameFrameDirsAllowed = sameFrameDirsAllowed
        };
        attempts.Add(a);
        building?.attempts.Add(a);
        return a;
    }

    public static void MarkAttack(Attempt a, int attackIdx) => a.attackIdx = attackIdx;
    public static void MarkMatch(Attempt a, int idx) => a.matchedIdx.Add(idx);
    public static void MarkGap(Attempt a, int gapsUsed) => a.gapsUsed = gapsUsed;
    public static void Success(Attempt a)
    {
        a.success = true; a.failReason = "";
        // ���� ���� �⺻ ���� (�ʿ��ϸ� ���� �ٲ㵵 OK)
        string idx = (a.matchedIdx != null && a.matchedIdx.Count > 0)
            ? string.Join(", ", a.matchedIdx)
            : "-";
        PushSuccessToast($"OK  {a.skillName}   [idx: {idx}]");
    }
    public static void Fail(Attempt a, string reason) { a.success = false; a.failReason = reason; }

    // �� ������ ��: �����丮�� Ŀ��
    public static void EndFrame()
    {
        if (Frozen || building == null) return; // �� ���� �߿� �����丮�� �� ������ �߰� ����
        history.Add(building);
        if (history.Count > HistoryCapacity) history.RemoveAt(0);
        building = null;
    }

    // �� ����
    public static void ToggleFreeze()
    {
        // �� ���� �����ӿ� �� �� ȣ��Ǹ� ����(��ٿ)
        if (Time.frameCount == _lastToggleFrame) return;
        _lastToggleFrame = Time.frameCount;

        Frozen = !Frozen;
        if (!Frozen) ViewIndex = -1;   // ���̺� ����
        else if (ViewIndex < 0) ViewIndex = Mathf.Max(0, history.Count - 1);

        // �� ���� ����� �����ӿ� �ڵ� ����� �������� �ʵ��� ����
        _suppressAutoFreezeThisFrame = true;
    }

    public static void Step(int delta)
    {
        if (building == null) return;
        history.Add(building);
        if (history.Count > HistoryCapacity) history.RemoveAt(0);
        building = null;

        // �� �ڵ� ������: ���� ������ �������� �н�
        if (AutoFreezeOnFail && !_suppressAutoFreezeThisFrame)
        {
            bool anyFail = history[^1].attempts.Exists(a => !a.success);
            if (anyFail)
            {
                Frozen = true;
                ViewIndex = history.Count - 1;
                freezeHoldCounter = AutoFreezeHoldFrames;
            }
        }
        _suppressAutoFreezeThisFrame = false; // ������ ���� �� ����
    }

    // �� �������̰� �׸� ������ ����
    public static FrameLog GetDisplayFrame()
    {
        if (Frozen && ViewIndex >= 0 && ViewIndex < history.Count)
            return history[ViewIndex];

        return new FrameLog
        {
            frame = Time.frameCount,
            time = Time.time,
            buffer = lastBuffer,
            attempts = new List<Attempt>(attempts)
        };
    }

    public static void EnterFreeze()
    {
        Frozen = true;
        if (history.Count > 0) ViewIndex = history.Count - 1;
        // freezeHoldCounter ���� �ڵ� ������ Ÿ�̸Ӵ� ��� �� ��
    }

    public static void ExitFreezeToLive()
    {
        Frozen = false;
        ViewIndex = -1; // ���̺�
    }
}
