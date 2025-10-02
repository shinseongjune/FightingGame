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
        // ����: tickTimer += Time.deltaTime; ...
        // ����: TimeController�� timeScale�� �ݿ� (���ο�/������ ���� ó��)
        float scaledDelta = Time.deltaTime;

        // �����ϰ� TimeController�� ���� ��� �⺻ 1.0 ���
        if (TimeController.Instance != null)
        {
            // GetTimeScaleForTicks�� hitstop/slowmotion �ݿ�
            float tmScale = TimeController.Instance.GetTimeScaleForTicks();
            scaledDelta = Time.deltaTime * tmScale;

            // ��Ʈ��ž(������ ��ŵ) ����: TimeController�� ������ ������ timeScale�� 0���� ����� scaledDelta == 0 �� �Ǿ� tickTimer�� �������� ����.
            // �ٸ� ��Ʈ��ž�� '������ ���� ��ŵ'���� �����ϰ��� �Ѵٸ� TimeController�� IsInHitstop �÷��׸� üũ�� Tick ���� ��ü�� �ǳʶ� ���� ����.
        }

        tickTimer += scaledDelta;

        int ticksThisFrame = 0;
        while (tickTimer >= TICK_INTERVAL && ticksThisFrame < maxTicksPerFrame)
        {
            tickTimer -= TICK_INTERVAL;

            // ���� ƽ ȣ��
            for (int i = 0; i < tickers.Count; i++)
            {
                if (tickers[i] != null)
                    tickers[i].Tick();
            }

            ticksThisFrame++;
        }

        // ��ó��(��� ���� ��)
        foreach (var t in pendingRemove)
        {
            tickers.Remove(t);
        }
        pendingRemove.Clear();
    }
}
