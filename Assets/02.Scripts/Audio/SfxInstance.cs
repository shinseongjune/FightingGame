using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public sealed class SfxInstance : MonoBehaviour
{
    private System.Action<SfxInstance> _onDespawn;
    private AudioSource _src;

    void Awake()
    {
        if (!_src) _src = GetComponent<AudioSource>();
        _src.playOnAwake = false;
    }

    public void Configure(SfxEntry_SO entry)
    {
        if (!_src) _src = GetComponent<AudioSource>();
        _src.clip = entry.clip;
        _src.outputAudioMixerGroup = entry.outputGroup;
        _src.spatialBlend = entry.is2D ? 0f : 1f; // 2D/3D
        _src.volume = entry.defaultVolume;
        _src.pitch = entry.defaultPitch;
        _src.loop = false;
    }

    public void Play(SfxEntry_SO entry, Vector3 pos, Transform follow, float volumeMul, float pitchMul, System.Action<SfxInstance> onDespawn)
    {
        _onDespawn = onDespawn;
        Configure(entry);

        // 지터(무작위 피치 가감)
        if (entry.pitchJitter > 0f)
        {
            _src.pitch *= 1f + Random.Range(-entry.pitchJitter, entry.pitchJitter);
        }
        _src.pitch *= Mathf.Max(0.1f, pitchMul);
        _src.volume *= Mathf.Clamp01(volumeMul);

        if (follow != null)
        {
            transform.SetParent(follow, worldPositionStays: false);
            transform.localPosition = Vector3.zero;
        }
        else
        {
            transform.SetParent(null);
            transform.position = pos;
        }

        gameObject.SetActive(true);
        _src.Play();

        // 원샷 수명 관리(클립 길이 후 반환)
        CancelInvoke(nameof(Despawn));
        Invoke(nameof(Despawn), Mathf.Max(0.01f, _src.clip ? _src.clip.length / _src.pitch : 0.2f));
    }

    public void Despawn()
    {
        _src.Stop();
        gameObject.SetActive(false);
        _onDespawn?.Invoke(this);
        _onDespawn = null;
    }
}
