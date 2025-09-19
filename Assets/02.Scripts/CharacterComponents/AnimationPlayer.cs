using System;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[RequireComponent(typeof(Animator))]
public class AnimationPlayer : MonoBehaviour
{
    private Animator animator;
    private PlayableGraph graph;
    private AnimationClipPlayable clipPlayable;
    private AnimationClip currentClip;

    // 시간/프레임
    private float currentTimeSec;              // 현재 재생 시간(초)
    private float clipFps = 60f;               // 현재 클립 샘플레이트(fps)
    public float CurrentTimeSec => currentTimeSec;
    public int CurrentClipFrame => Mathf.FloorToInt(currentTimeSec * clipFps + 1e-4f);
    public int CurrentTickFrame => Mathf.FloorToInt(currentTimeSec / TickMaster.TICK_INTERVAL + 1e-4f);
    public int ClipLengthFrames => currentClip != null ? Mathf.RoundToInt(currentClip.length * clipFps) : 0;

    public string CurrentClipName => currentClip != null ? currentClip.name : string.Empty;

    private Action onComplete;
    private bool loop;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        graph = PlayableGraph.Create($"{name}_AnimGraph");
        var output = AnimationPlayableOutput.Create(graph, "AnimOutput", animator);
        clipPlayable = AnimationClipPlayable.Create(graph, new AnimationClip());
        output.SetSourcePlayable(clipPlayable);
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
    }

    private void OnDisable()
    { 
        CleanupPlayable(); 
    }

    void OnDestroy()
    {
        CleanupPlayable();
    }

    void OnApplicationQuit()
    {
        CleanupPlayable();
    }

    private void CleanupPlayable()
    {
        if (graph.IsValid())
        {
            graph.Destroy();
        }
    }

    public bool Play(string clipKey, Action onComplete = null, bool loop = false)
    {
        var clip = AnimationClipLibrary.Instance.Get(clipKey);
        if (clip == null)
        {
            Debug.LogWarning($"[AnimationPlayer] Clip not found: {clipKey}");
            return false;
        }

        currentClip = clip;
        clipFps = Mathf.Max(1f, clip.frameRate);
        currentTimeSec = 0f;
        this.onComplete = onComplete;

        this.loop = loop || clip.isLooping;

        if (!graph.IsValid())
        {
            graph = PlayableGraph.Create($"{name}_AnimGraph");
            var output = AnimationPlayableOutput.Create(graph, "AnimOutput", animator);
            clipPlayable = AnimationClipPlayable.Create(graph, clip);
            output.SetSourcePlayable(clipPlayable);
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        }
        else
        {
            if (clipPlayable.IsValid()) clipPlayable.Destroy();
            clipPlayable = AnimationClipPlayable.Create(graph, clip);
            var output = (AnimationPlayableOutput)graph.GetOutput(0);
            output.SetSourcePlayable(clipPlayable);
        }

        clipPlayable.SetTime(0);
        clipPlayable.SetSpeed(0);
        if (!graph.IsPlaying()) graph.Play();

        return true;
    }

    public void Tick()
    {
        if (!graph.IsValid() || !clipPlayable.IsValid() || currentClip == null) return;

        currentTimeSec += TickMaster.TICK_INTERVAL;

        if (currentTimeSec >= currentClip.length)
        {
            if (loop)
            {
                currentTimeSec -= currentClip.length;            // 루프
                clipPlayable.SetTime(currentTimeSec);
                graph.Evaluate(0); // 즉시 반영
            }
            else
            {
                currentTimeSec = currentClip.length;
                clipPlayable.SetTime(currentTimeSec);
                graph.Evaluate(0);

                var cb = onComplete;
                onComplete = null;

                currentClip = null;
                clipPlayable.SetSpeed(0);

                cb?.Invoke();
                return;
            }
        }
        else
        {
            clipPlayable.SetTime(currentTimeSec);
            graph.Evaluate(0);
        }
    }
}
