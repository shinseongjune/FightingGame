using UnityEngine;

[CreateAssetMenu(fileName = "new Effect Entry", menuName = "SO/Effect Entry")]
public sealed class EffectEntry_SO : ScriptableObject
{
    [Tooltip("Spawn 시 사용할 키 (중복 금지)")]
    public string key;

    [Tooltip("이펙트 프리팹 (Particle/스프라이트 등)")]
    public GameObject prefab;

    [Tooltip("기본 생존 시간(초). 0이면 ParticleSystem 종료 이벤트로 회수")]
    public float defaultLifetime = 1.0f;

    [Tooltip("사전 예열 개수(풀)")]
    public int prewarm = 4;
}