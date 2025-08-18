using System.Collections.Generic;
using UnityEngine;

public class TickMaster : Singleton<TickMaster>
{
    public const float TICK_INTERVAL = 1f / 60f;
    private float tickTimer;
    int maxTicksPerFrame = 5;

    private readonly List<ITicker> tickers = new();

    private readonly HashSet<ITicker> pendingRemove = new();

    public void Register(ITicker t)
    {
        if (t == null || tickers.Contains(t)) return;
        tickers.Add(t);
    }

    public void Unregister(ITicker t)
    {
        pendingRemove.Add(t);
    }

    void Update()
    {
        if (GamePause.IsPaused) return;

        tickTimer += Time.deltaTime;

        int ticksThisFrame = 0;

        while (tickTimer >= TICK_INTERVAL && ticksThisFrame < maxTicksPerFrame)
        {
            tickTimer -= TICK_INTERVAL;
            for (int i = 0; i < tickers.Count; i++)
            {
                if (tickers[i] != null)
                    tickers[i].Tick();
            }

            ticksThisFrame++;
        }

        foreach (var t in pendingRemove)
        {
            tickers.Remove(t);
        }
        pendingRemove.Clear();

        RecognizerTrace.EndFrame();
    }
}
