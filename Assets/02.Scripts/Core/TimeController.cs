using UnityEngine;
using System.Collections;

/// <summary>
/// 중앙 시간 제어기: 히트스탑(프레임 단위) / 슬로우모션(초 단위) 등을 관리.
/// TickMaster는 이 클래스의 값을 이용해 틱을 누적/스킵함.
/// - 히트스탑은 "프레임(=Unity Update 프레임) 단위"로 감소시킴 (정확한 틱 단위 재현성 필요시 틱카운트 기반으로 바꿀 수 있음)
/// </summary>
public class TimeController : Singleton<TimeController>
{
    // 현재 전체 게임 틱에 곱할 스케일 (기본 1.0)
    public float timeScale { get; private set; } = 1f;

    // 슬로우모션 유지 타이머 (초 단위, realtime 기반)
    private float slowMotionTimerSec = 0f;
    private float slowMotionTargetScale = 1f;

    // 히트스탑: 남은 프레임 수 (Unity Update 프레임 단위)
    private int hitStopFramesRemaining = 0;

    // 우선순위/중첩을 간단히 처리하기 위한 카운터
    // (더 강력한 시스템을 원하면 priority / stack list 를 구현)
    public bool IsInHitstop => hitStopFramesRemaining > 0;

    void Update()
    {
        // 히트스탑이 우선 — hitstop이 존재하면 slowMotionTimer는 계속 감소시키되 timeScale은 0
        if (hitStopFramesRemaining > 0)
        {
            // hitstop은 Update()가 매프레임 호출된다고 가정 -> 프레임 단위 감소
            hitStopFramesRemaining--;
            // timeScale은 0으로 취급 (실제 틱 실행에서는 TickMaster가 체크)
            // 다만 slowMotionTimer를 같이 감소시킬지 여부는 디자인 선택; 여기서는 함께 감소시킴:
            if (slowMotionTimerSec > 0f)
            {
                slowMotionTimerSec -= Time.unscaledDeltaTime;
                if (slowMotionTimerSec <= 0f)
                {
                    slowMotionTimerSec = 0f;
                    slowMotionTargetScale = 1f;
                }
            }
            timeScale = 0f;
            return;
        }

        // 슬로우모션 타이머 처리
        if (slowMotionTimerSec > 0f)
        {
            slowMotionTimerSec -= Time.unscaledDeltaTime;
            if (slowMotionTimerSec <= 0f)
            {
                slowMotionTimerSec = 0f;
                slowMotionTargetScale = 1f;
            }
            timeScale = Mathf.Clamp01(slowMotionTargetScale);
        }
        else
        {
            timeScale = 1f;
        }
    }

    /// <summary>
    /// 즉시 히트스탑을 적용 (프레임 단위)
    /// e.g. ApplyHitstop(3) -> 다음 3 Update 프레임 동안 틱 실행(게임플레이) 정지
    /// </summary>
    public void ApplyHitstop(int frames)
    {
        if (frames <= 0) return;
        // 누적된 hitstop은 최대값을 취하도록 (원하면 더 복잡한 스택 로직 적용)
        hitStopFramesRemaining = Mathf.Max(hitStopFramesRemaining, frames);
    }

    /// <summary>
    /// 슬로우모션 적용: scale (0..1), duration seconds (real time)
    /// scale == 0 은 사실상 멈춤과 유사하므로 ApplyHitstop가 더 적절.
    /// </summary>
    public void ApplySlowMotion(float scale, float seconds)
    {
        slowMotionTargetScale = Mathf.Clamp(scale, 0f, 1f);
        slowMotionTimerSec = Mathf.Max(slowMotionTimerSec, seconds);
        // timeScale는 Update에서 반영
    }

    /// <summary>
    /// TickMaster에서 틱 누적을 할 때, deltaTime에 곱할 값
    /// </summary>
    public float GetTimeScaleForTicks()
    {
        // 히트스탑이 존재하면 0 반환
        if (hitStopFramesRemaining > 0) return 0f;
        return timeScale;
    }

    // 강제 해제(테스트/디버그용)
    public void ClearAllTemporalEffects()
    {
        hitStopFramesRemaining = 0;
        slowMotionTimerSec = 0f;
        slowMotionTargetScale = 1f;
        timeScale = 1f;
    }
}
