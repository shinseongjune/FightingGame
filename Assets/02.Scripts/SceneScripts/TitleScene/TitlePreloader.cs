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
        // �ڵ� ������ �ȴٸ� Start�� ����, �ܺο��� BeginPreload()�� ȣ���ϰ� �ٲ㵵 �˴ϴ�.
        await BeginPreload();
    }

    public async Task BeginPreload()
    {
        if (IsPreloading || IsReady) return;

        IsPreloading = true;
        IsReady = false;
        Progress = 0f;
        UpdateUI();

        // 1) Ű ����
        var stageKeys = CollectStageKeys(stageCatalog);                    // "Stage/<�̸�>"
        var charKeys = CollectCharacterKeys(characterCatalog);            // "Character/<�̸�>/<����>"
        var animKeys = CollectAnimationClipKeys(characterCatalog);        // clipKey

        // 2) ���� �����ε�
        var tAnim = AnimationClipLibrary.Instance.LoadAssetsAsync(animKeys);
        var tStage = StageLibrary.Instance.PreloadAsync(stageKeys);
        var tChar = CharacterLibrary.Instance.PreloadAsync(charKeys);

        // 3) ���� �� ������Ʈ
        while (!tAnim.IsCompleted || !tStage.IsCompleted || !tChar.IsCompleted)
        {
            Progress = (AnimationClipLibrary.Instance.LoadProgress +
                        StageLibrary.Instance.LoadProgress +
                        CharacterLibrary.Instance.LoadProgress) / 3f;
            UpdateUI();
            await Task.Yield();
        }

        // 4) �Ϸ� �÷���/�̺�Ʈ��
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

    // --- Collectors (SO �ʵ�� ���� �ʿ�� �̸��� �ٲ��ּ���) ---
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
            // 1) AnimSet ��� Ű
            if (e.animSet?.entries != null)
            {
                foreach (var a in e.animSet.entries)
                {
                    if (!string.IsNullOrEmpty(a.clipKey))
                        set.Add($"{e.characterName}/{a.clipKey}");
                }
            }

            // 2) Extra Clip Keys �߰�
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
