using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public sealed class TitlePreloader : MonoBehaviour
{
    [Header("Catalogs")]
    [SerializeField] private CharacterCatalog_SO characterCatalog;
    [SerializeField] private StageCatalog_SO stageCatalog;

    [Header("Optional UI")]
    [SerializeField] private UnityEngine.UI.Slider progressBar;

    [Header("Events")]
    public UnityEvent onPreloadCompleted;

    public bool IsPreloading { get; private set; }
    public bool IsReady { get; private set; }
    public float Progress { get; private set; }  // 0~1

    public GameObject loadingPanel;

    private async void Start()
    {
        // 자동 시작이 싫다면 Start를 비우고, 외부에서 BeginPreload()를 호출하게 바꿔도 됩니다.
        await BeginPreload();
    }

    public async Task BeginPreload()
    {
        if (IsPreloading || IsReady) return;

        IsPreloading = true;
        IsReady = false;
        Progress = 0f;
        UpdateUI();

        // 1) 키 수집
        var stageKeys = CollectStageKeys(stageCatalog);                    // "Stage/<이름>"
        var charKeys = CollectCharacterKeys(characterCatalog);            // "Character/<이름>/<색상>"
        var animKeys = CollectAnimationClipKeys(characterCatalog);        // clipKey

        // 2) 병렬 프리로드
        var tAnim = AnimationClipLibrary.Instance.LoadAssetsAsync(animKeys);
        var tStage = StageLibrary.Instance.PreloadAsync(stageKeys);
        var tChar = CharacterLibrary.Instance.PreloadAsync(charKeys);

        // 3) 진행 바 업데이트
        while (!tAnim.IsCompleted || !tStage.IsCompleted || !tChar.IsCompleted)
        {
            Progress = (AnimationClipLibrary.Instance.LoadProgress +
                        StageLibrary.Instance.LoadProgress +
                        CharacterLibrary.Instance.LoadProgress) / 3f;
            UpdateUI();
            await Task.Yield();
        }

        // 4) 완료 플래그/이벤트만
        Progress = 1f;
        UpdateUI();
        IsPreloading = false;
        IsReady = true;
        onPreloadCompleted?.Invoke();

        loadingPanel.SetActive(false);
    }

    void UpdateUI()
    {
        if (progressBar) progressBar.value = Progress;
    }

    // --- Collectors (SO 필드명에 맞춰 필요시 이름만 바꿔주세요) ---
    static List<string> CollectStageKeys(StageCatalog_SO sc)
        => sc?.entries?.Select(e => $"Stage/{e.stageName}")?.Distinct().ToList() ?? new();

    static List<string> CollectCharacterKeys(CharacterCatalog_SO cc)
    {
        var keys = new List<string>();
        if (cc?.entries == null) return keys;
        foreach (var e in cc.entries)
        {
            int colorCount = Mathf.Max(1, e.colorCount);
            for (int i = 0; i < colorCount; i++)
                keys.Add($"Character/{e.characterName}/{i}");
        }
        return keys.Distinct().ToList();
    }

    static List<string> CollectAnimationClipKeys(CharacterCatalog_SO cc)
    {
        var set = new HashSet<string>();
        if (cc?.entries == null) return set.ToList();

        foreach (var e in cc.entries)
        {
            // 1) AnimSet 기반 키
            if (e.animSet?.entries != null)
            {
                foreach (var a in e.animSet.entries)
                {
                    if (!string.IsNullOrEmpty(a.clipKey))
                        set.Add($"{e.characterName}/{a.clipKey}");
                }
            }

            // 2) Extra Clip Keys 추가
            if (e.extraClipKeys != null)
            {
                foreach (var extra in e.extraClipKeys)
                {
                    if (!string.IsNullOrEmpty(extra))
                        set.Add($"{e.characterName}/{extra}");
                }
            }
        }
        return set.ToList();
    }
}
