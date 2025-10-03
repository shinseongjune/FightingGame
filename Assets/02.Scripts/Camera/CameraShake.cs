// Camera/CameraShake.cs
using UnityEngine;

[DefaultExecutionOrder(10000)]
public sealed class CameraShake : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CameraRig_25D rig;

    [Header("Strength")]
    [SerializeField] private float maxYOffset = 0.35f;   // 상하 흔들림 크기
    [SerializeField] private float maxXOffset = 0.08f;   // 좌우는 조금만

    [Header("Dynamics")]
    [SerializeField] private float decayPerSec = 30f;    // 빨리 감쇠
    [SerializeField] private float noiseFreqY = 70f;     // 상하 주파수 (빠르게)
    [SerializeField] private float noiseFreqX = 30f;     // 좌우 주파수 (느리게)

    private float trauma;
    private float queuedTrauma;
    private float t;
    private int seed;
    private bool wasHitStop;

    void Reset()
    {
        if (rig == null) rig = FindFirstObjectByType<CameraRig_25D>();
    }

    void Awake()
    {
        if (rig == null) rig = FindFirstObjectByType<CameraRig_25D>();
        seed = 9876543;
    }

    public void Clear()
    {
        trauma = 0f;
        queuedTrauma = 0f;
        t = 0f;
    }

    public void Impulse(float magnitude, float duration = 0.08f, int extraSeed = 0)
    {
        magnitude = Mathf.Clamp01(magnitude);
        queuedTrauma = Mathf.Clamp01(Mathf.Max(queuedTrauma, magnitude));
        if (extraSeed != 0) seed ^= extraSeed;

        if (IsHitStopActive())
        {
            trauma = Mathf.Clamp01(Mathf.Max(trauma, queuedTrauma));
        }
    }

    void LateUpdate()
    {
        bool inHitStop = IsHitStopActive();

        if (inHitStop && !wasHitStop)
        {
            trauma = Mathf.Clamp01(Mathf.Max(trauma, queuedTrauma));
            queuedTrauma = 0f;
            t = 0f;
        }

        if (inHitStop)
        {
            if (rig != null && trauma > 0f)
            {
                float dt = Time.unscaledDeltaTime;
                t += dt;
                trauma = Mathf.Max(0f, trauma - decayPerSec * dt);

                float p = trauma * trauma;

                // 좌우는 살짝, 상하는 바쁘게
                float nx = HashSin(seed + 11, t * noiseFreqX);
                float ny = HashSin(seed + 37, t * noiseFreqY);

                Vector2 offset = new Vector2(nx * maxXOffset * p,
                                             ny * maxYOffset * p);

                rig.ApplyExternalShake(offset, 0f); // roll은 제거
            }
        }
        else
        {
            if (wasHitStop)
            {
                trauma = 0f;
            }
        }

        wasHitStop = inHitStop;
    }

    private static bool IsHitStopActive()
    {
        return TimeController.TryGet(out var tc) && tc.IsInHitstop;
    }

    private static float HashSin(int s, float x)
    {
        float w = Mathf.Sin(x + (s * 0.12345f)) + Mathf.Sin(x * 1.7f + s * 0.34567f);
        return Mathf.Clamp(w * 0.5f, -1f, 1f);
    }
}