using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public sealed class SoundService : MonoBehaviour
{
    [Header("Libraries")]
    [SerializeField] private AudioLibrary_SO sfxLibrary;

    [Header("Music")]
    [SerializeField] private AudioMixer masterMixer;
    [SerializeField] private string bgmVolumeParam = "BGM_dB";
    [SerializeField] private string sfxVolumeParam = "SFX_dB";
    [SerializeField] private AudioSource bgmA; // Crossfade용 두 트랙
    [SerializeField] private AudioSource bgmB;
    [SerializeField] private float bgmCrossfade = 0.25f;

    private readonly Dictionary<string, Queue<SfxInstance>> pools = new();
    private readonly Dictionary<string, Transform> poolRoots = new();

    public static SoundService Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (sfxLibrary != null) sfxLibrary.BuildMap();
        if (!bgmA) bgmA = gameObject.AddComponent<AudioSource>();
        if (!bgmB) bgmB = gameObject.AddComponent<AudioSource>();
        bgmA.loop = true; bgmB.loop = true;
        bgmA.playOnAwake = false; bgmB.playOnAwake = false;
    }

    // ---------- SFX ----------
    public void Prewarm(string key)
    {
        if (!TryGetEntry(key, out var entry)) return;
        EnsurePool(key, entry);
    }

    public void PlayKey(string key, float volumeMul = 1f, float pitchMul = 1f)
        => PlayInternal(key, Vector3.zero, null, volumeMul, pitchMul);

    public void PlayAt(string key, Vector3 pos, float volumeMul = 1f, float pitchMul = 1f)
        => PlayInternal(key, pos, null, volumeMul, pitchMul);

    public void PlayAttached(string key, Transform follow, float volumeMul = 1f, float pitchMul = 1f)
        => PlayInternal(key, Vector3.zero, follow, volumeMul, pitchMul);

    private void PlayInternal(string key, Vector3 pos, Transform follow, float volumeMul, float pitchMul)
    {
        if (!TryGetEntry(key, out var entry) || entry.clip == null) return;
        var inst = Rent(key, entry);
        inst.Play(entry, pos, follow, volumeMul, pitchMul, (i) => Return(key, i));
    }

    private bool TryGetEntry(string key, out SfxEntry_SO entry)
    {
        entry = null;
        return sfxLibrary != null && sfxLibrary.TryGet(key, out entry);
    }

    private void EnsurePool(string key, SfxEntry_SO entry)
    {
        if (!pools.TryGetValue(key, out var q))
        {
            q = new Queue<SfxInstance>(entry.prewarm);
            pools[key] = q;

            var root = new GameObject($"SFXPool_{key}").transform;
            root.SetParent(transform);
            poolRoots[key] = root;

            for (int i = 0; i < entry.prewarm; ++i)
            {
                var go = new GameObject($"SFX_{key}_{i}");
                go.transform.SetParent(root);
                var s = go.AddComponent<SfxInstance>();
                s.Configure(entry);
                go.SetActive(false);
                q.Enqueue(s);
            }
        }
    }

    private SfxInstance Rent(string key, SfxEntry_SO entry)
    {
        EnsurePool(key, entry);
        var q = pools[key];
        if (q.Count > 0) return q.Dequeue();

        var go = new GameObject($"SFX_{key}_{pools[key].Count + 1}");
        go.transform.SetParent(poolRoots[key]);
        var s = go.AddComponent<SfxInstance>();
        s.Configure(entry);
        go.SetActive(false);
        return s;
    }

    private void Return(string key, SfxInstance inst)
    {
        inst.gameObject.SetActive(false);
        pools[key].Enqueue(inst);
    }

    // ---------- Music ----------
    public void PlayBGM(AudioClip clip, AudioMixerGroup output, float targetVolume = 1f, bool immediate = false)
    {
        var (from, to) = bgmA.isPlaying ? (bgmA, bgmB) : (bgmB, bgmA);
        to.clip = clip;
        to.outputAudioMixerGroup = output;
        to.volume = immediate ? targetVolume : 0f;
        to.Play();

        if (immediate)
        {
            from.Stop();
            from.clip = null;
            return;
        }
        StopAllCoroutines();
        StartCoroutine(Crossfade(from, to, targetVolume));
    }

    private System.Collections.IEnumerator Crossfade(AudioSource from, AudioSource to, float target)
    {
        float t = 0f;
        while (t < bgmCrossfade)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / bgmCrossfade);
            to.volume = Mathf.Lerp(0f, target, k);
            from.volume = Mathf.Lerp(target, 0f, k);
            yield return null;
        }
        from.Stop();
        from.clip = null;
        to.volume = target;
    }

    // ---------- Mixer Volumes ----------
    public void SetBGMdB(float dB) => masterMixer?.SetFloat(bgmVolumeParam, dB);
    public void SetSFXdB(float dB) => masterMixer?.SetFloat(sfxVolumeParam, dB);
}
