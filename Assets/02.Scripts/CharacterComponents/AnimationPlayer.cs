using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

public class AnimationPlayer : MonoBehaviour, ITicker
{
    private PlayableGraph graph;
    private AnimationPlayableOutput output;
    private AnimationClipPlayable currentPlayable;
    private Animator animator;

    private float currentTime = 0f;
    private float clipLength = 0f;
    private bool playing = false;

    private string currentKey;
    private Action onCompleteCallback;

    private const float TickDuration = TickMaster.TICK_INTERVAL;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        graph = PlayableGraph.Create("AnimationPlayerGraph");
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

        output = AnimationPlayableOutput.Create(graph, "AnimationOutput", animator);
    }

    public void Play(string clipKey, Action onComplete = null)
    {
        AnimationClip clip = AnimationClipLibrary.Instance.Get(clipKey);
        if (clip == null)
        {
            Debug.LogWarning($"[AnimationPlayer] Clip not found: {clipKey}");
            return;
        }

        if (currentPlayable.IsValid())
        {
            currentPlayable.Destroy();
        }

        currentPlayable = AnimationClipPlayable.Create(graph, clip);
        currentPlayable.SetDuration(clip.length);
        currentPlayable.SetTime(0);
        currentPlayable.SetSpeed(0);

        output.SetSourcePlayable(currentPlayable);
        graph.Play();

        currentTime = 0f;
        clipLength = clip.length;
        playing = true;

        currentKey = clipKey;
        onCompleteCallback = onComplete;
    }

    public void Tick()
    {
        if (!playing) return;

        currentTime += TickDuration;

        if (currentPlayable.IsValid())
        {
            currentPlayable.SetTime(currentTime);
            graph.Evaluate(TickDuration);
        }

        if (currentTime >= clipLength)
        {
            playing = false;

            onCompleteCallback?.Invoke();
            onCompleteCallback = null;
        }
    }

    private void OnDestroy()
    {
        if (graph.IsValid())
        {
            graph.Destroy();
        }
    }
}
