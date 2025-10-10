using UnityEngine;

[CreateAssetMenu(fileName = "new Effect Entry", menuName = "SO/Effect Entry")]
public sealed class EffectEntry_SO : ScriptableObject
{
    [Tooltip("Spawn �� ����� Ű (�ߺ� ����)")]
    public string key;

    [Tooltip("����Ʈ ������ (Particle/��������Ʈ ��)")]
    public GameObject prefab;

    [Tooltip("�⺻ ���� �ð�(��). 0�̸� ParticleSystem ���� �̺�Ʈ�� ȸ��")]
    public float defaultLifetime = 1.0f;

    [Tooltip("���� ���� ����(Ǯ)")]
    public int prewarm = 4;
}