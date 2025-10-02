using System.Collections.Generic;
using UnityEngine;

public class TickMaster : Singleton<TickMaster>
{
    public const float TICK_INTERVAL = 1f / 60f;
    private float tickTimer;
    int maxTicksPerFrame = 5;

    private readonly List<ITicker> tickers = new();

    private readonly HashSet<ITicker> pendingRemove = new();

    public bool IsReady { get; private set; }

    protected override bool ShouldPersistAcrossScenes() => false;

    public void Register(ITicker t)
    {
        if (t == null || tickers.Contains(t)) return;
        tickers.Add(t);
    }

    public void Unregister(ITicker t)
    {
        pendingRemove.Add(t);
    }

    protected override void Awake()
    {
        base.Awake();
        IsReady = false;
    }

    void Start()
    {
        IsReady = true;
    }

    void Update()
    {
        // 기존: tickTimer += Time.deltaTime; ...
        // 변경: TimeController의 timeScale을 반영 (슬로우/프레임 멈춤 처리)
        float scaledDelta = Time.deltaTime;

        // 안전하게 TimeController가 없을 경우 기본 1.0 사용
        if (TimeController.Instance != null)
        {
            // GetTimeScaleForTicks은 hitstop/slowmotion 반영
            float tmScale = TimeController.Instance.GetTimeScaleForTicks();
            scaledDelta = Time.deltaTime * tmScale;

            // 히트스탑(프레임 스킵) 동작: TimeController이 프레임 단위로 timeScale을 0으로 만들면 scaledDelta == 0 이 되어 tickTimer가 증가하지 않음.
            // 다만 히트스탑을 '프레임 완전 스킵'으로 구현하고자 한다면 TimeController의 IsInHitstop 플래그를 체크해 Tick 실행 자체를 건너뛸 수도 있음.
        }

        tickTimer += scaledDelta;

        int ticksThisFrame = 0;
        while (tickTimer >= TICK_INTERVAL && ticksThisFrame < maxTicksPerFrame)
        {
            tickTimer -= TICK_INTERVAL;

            // 실제 틱 호출
            for (int i = 0; i < tickers.Count; i++)
            {
                if (tickers[i] != null)
                    tickers[i].Tick();
            }

            ticksThisFrame++;
        }

        // 후처리(대기 제거 등)
        foreach (var t in pendingRemove)
        {
            tickers.Remove(t);
        }
        pendingRemove.Clear();
    }
}
