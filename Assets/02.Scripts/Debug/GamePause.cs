using UnityEngine;

public static class GamePause
{
    public static bool IsPaused { get; private set; }

    public static void Toggle()
    {
        if (IsPaused) Resume();
        else Pause();
    }

    public static void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;
        Time.timeScale = 0f;                  // 애니/Physics 정지
        RecognizerTrace.EnterFreeze();        // 마지막 프레임을 화면에 유지
    }

    public static void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;
        Time.timeScale = 1f;
        RecognizerTrace.ExitFreezeToLive();   // 라이브 모드 복귀
    }
}