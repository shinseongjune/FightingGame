using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "new SFX Entry", menuName = "SO/SFX Entry")]
public sealed class SfxEntry_SO : ScriptableObject
{
    [Tooltip("����� ����� ���� Ű (�ߺ� ����)")]
    public string key;

    [Tooltip("AudioClip �Ǵ� ������(3D��)�� �ִ� ���, ���⼭�� Clip�� ���� ����")]
    public AudioClip clip;

    [Tooltip("�⺻ ����(0~1)")]
    [Range(0f, 1f)] public float defaultVolume = 1f;

    [Tooltip("�⺻ ��ġ(0.1~3)")]
    [Range(0.1f, 3f)] public float defaultPitch = 1f;

    [Tooltip("��� �� ��ġ ������ ���� ����(����)")]
    [Range(0f, 0.5f)] public float pitchJitter = 0.0f;

    [Tooltip("2D�� ó��(SpatialBlend=0). 3D�� �ʿ��ϸ� false�� �ϰ� 3D ����")]
    public bool is2D = true;

    [Tooltip("���� Ǯ�� ����")]
    public int prewarm = 4;

    [Tooltip("���� MixerGroup (Master/SFX/BGM ��)")]
    public AudioMixerGroup outputGroup;
}
