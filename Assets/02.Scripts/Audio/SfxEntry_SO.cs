using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "new SFX Entry", menuName = "SO/SFX Entry")]
public sealed class SfxEntry_SO : ScriptableObject
{
    [Tooltip("재생에 사용할 고유 키 (중복 방지)")]
    public string key;

    [Tooltip("AudioClip 또는 프리팹(3D용)을 넣는 대신, 여기서는 Clip을 직접 참조")]
    public AudioClip clip;

    [Tooltip("기본 볼륨(0~1)")]
    [Range(0f, 1f)] public float defaultVolume = 1f;

    [Tooltip("기본 피치(0.1~3)")]
    [Range(0.1f, 3f)] public float defaultPitch = 1f;

    [Tooltip("재생 시 피치 무작위 가감 범위(±값)")]
    [Range(0f, 0.5f)] public float pitchJitter = 0.0f;

    [Tooltip("2D로 처리(SpatialBlend=0). 3D가 필요하면 false로 하고 3D 세팅")]
    public bool is2D = true;

    [Tooltip("사전 풀링 수량")]
    public int prewarm = 4;

    [Tooltip("보낼 MixerGroup (Master/SFX/BGM 등)")]
    public AudioMixerGroup outputGroup;
}
