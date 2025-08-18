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
        Time.timeScale = 0f;                  // �ִ�/Physics ����
        RecognizerTrace.EnterFreeze();        // ������ �������� ȭ�鿡 ����
    }

    public static void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;
        Time.timeScale = 1f;
        RecognizerTrace.ExitFreezeToLive();   // ���̺� ��� ����
    }
}