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

    // ★ 프레임 스냅샷
    [Serializable]
    public class FrameLog
    {
        public int frame;
        public float time;
        public InputData[] buffer;
        public List<Attempt> attempts;
    }

    // 라이브(현재 프레임) 버퍼
    public static InputData[] lastBuffer = Array.Empty<InputData>();
    public static readonly List<Attempt> attempts = new();

    // ★ 히스토리(링버퍼)
    static readonly List<FrameLog> history = new();
    public static int HistoryCapacity = 180;          // 최근 3초(60fps 가정)
    static FrameLog building;                          // 현재 프레임 빌드 중

    // ★ 보기 상태
    public static bool Frozen { get; private set; } = false;
    public static int ViewIndex { get; private set; } = -1; // -1=라이브, 그 외: history 인덱스

    // ★ 자동 프리즈 옵션
    public static bool AutoFreezeOnFail = true;
    public static int AutoFreezeHoldFrames = 30;       // 실패 시 0.5초 유지(60fps 가정)
    static int freezeHoldCounter = 0;

    // ▶ 성공 토스트 설정
    public static float SuccessToastDuration = 2.0f; // 몇 초 보여줄지
    public static int SuccessToastMax = 6;           // 화면에 동시에 보여줄 최대 개수

    // ▶ 토스트 데이터
    public struct SuccessToast
    {
        public string text;
        public float t0Unscaled; // Time.unscaledTime 기준
    }
    public static readonly List<SuccessToast> successToasts = new();

    // ▶ 토스트 추가 함수
    public static void PushSuccessToast(string text)
    {
        successToasts.Add(new SuccessToast { text = text, t0Unscaled = Time.unscaledTime });
        // 너무 많아지면 오래된 것부터 제거
        while (successToasts.Count > SuccessToastMax) successToasts.RemoveAt(0);
    }

    // 프레임 시작: 라이브 버퍼 기록 + 빌딩 시작
    public static void BeginFrame(InputData[] bufferSnapshot)
    {
        if (Frozen) return; // ★ 정지 중엔 라이브 스냅샷/시도 초기화 금지
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

    // 시도 시작/마킹
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
        // 보기 좋은 기본 문구 (필요하면 포맷 바꿔도 OK)
        string idx = (a.matchedIdx != null && a.matchedIdx.Count > 0)
            ? string.Join(", ", a.matchedIdx)
            : "-";
        PushSuccessToast($"OK  {a.skillName}   [idx: {idx}]");
    }
    public static void Fail(Attempt a, string reason) { a.success = false; a.failReason = reason; }

    // ★ 프레임 끝: 히스토리에 커밋
    public static void EndFrame()
    {
        if (Frozen || building == null) return; // ★ 정지 중엔 히스토리에 새 프레임 추가 금지
        history.Add(building);
        if (history.Count > HistoryCapacity) history.RemoveAt(0);
        building = null;
    }

    // ★ 조작
    public static void ToggleFreeze()
    {
        // ★ 같은 프레임에 두 번 호출되면 무시(디바운스)
        if (Time.frameCount == _lastToggleFrame) return;
        _lastToggleFrame = Time.frameCount;

        Frozen = !Frozen;
        if (!Frozen) ViewIndex = -1;   // 라이브 복귀
        else if (ViewIndex < 0) ViewIndex = Mathf.Max(0, history.Count - 1);

        // ★ 수동 토글한 프레임엔 자동 프리즈가 개입하지 않도록 억제
        _suppressAutoFreezeThisFrame = true;
    }

    public static void Step(int delta)
    {
        if (building == null) return;
        history.Add(building);
        if (history.Count > HistoryCapacity) history.RemoveAt(0);
        building = null;

        // ★ 자동 프리즈: 수동 조작한 프레임은 패스
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
        _suppressAutoFreezeThisFrame = false; // 프레임 종료 시 해제
    }

    // ★ 오버레이가 그릴 데이터 제공
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
        // freezeHoldCounter 같은 자동 프리즈 타이머는 사용 안 함
    }

    public static void ExitFreezeToLive()
    {
        Frozen = false;
        ViewIndex = -1; // 라이브
    }
}
