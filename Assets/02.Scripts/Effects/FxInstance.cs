using UnityEngine;

public sealed class FxInstance : MonoBehaviour
{
    private float lifeRemain = -1f;
    private System.Action<FxInstance> _onDespawn;

    public void Play(float lifetime, System.Action<FxInstance> onDespawn)
    {
        _onDespawn = onDespawn;
        lifeRemain = lifetime;
        gameObject.SetActive(true);

        // 파티클이면 재생
        if (TryGetComponent<ParticleSystem>(out var ps)) ps.Play(true);
    }

    private void Update()
    {
        if (lifeRemain < 0f) return;
        lifeRemain -= Time.unscaledDeltaTime;
        if (lifeRemain <= 0f) Despawn();
    }

    private void OnParticleSystemStopped()
    {
        // lifetime=0으로 둔 항목은 파티클 종료로 회수
        if (lifeRemain == 0f) Despawn();
    }

    public void Despawn()
    {
        gameObject.SetActive(false);
        _onDespawn?.Invoke(this);
        _onDespawn = null;
    }
}