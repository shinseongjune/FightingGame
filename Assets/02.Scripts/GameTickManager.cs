using System;
using System.ComponentModel;
using UnityEngine;

public class GameTickManager : MonoBehaviour
{
    private static GameTickManager _instance;
    public static GameTickManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<GameTickManager>();

                if (_instance == null)
                {
                    GameObject go = new GameObject("GameTickManager");
                    _instance = go.AddComponent<GameTickManager>();
                }
            }

            return _instance;
        }
    }

    public int CurrentTick { get; private set; } = 0;
    public float FixedDeltaTime => Time.fixedDeltaTime;

    public event Action OnTick;

    [SerializeField] bool isStarted;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void FixedUpdate()
    {
        if (!isStarted)
            return;

        CurrentTick++;
        OnTick?.Invoke();
    }

    public void StartTick()
    {
        CurrentTick = 0;
        isStarted = true;
    }

    public void StopTick()
    {
        isStarted = false;
    }

    public void Subscribe(Action tickCallback)
    {
        OnTick += tickCallback;
    }

    public void Unsubscribe(Action tickCallback)
    {
        OnTick -= tickCallback;
    }
}
