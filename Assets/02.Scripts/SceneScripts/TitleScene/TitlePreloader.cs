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
        var stageKeys = CollectStageKeys(stageCatalog);               // "Stage/<�̸�>"
        var charKeys = CollectCharacterKeys(characterCatalog);       // "Character/<�̸�>/<����>"
        var animKeys = CollectAnimationClipKeys(characterCatalog);   // "<ĳ����>/<Ŭ��Ű>"
        var charPortraitKeys = CollectCharacterPortraitKeys(characterCatalog);// "Portrait/<ĳ����>"
        var stagePortraitKeys = CollectStagePortraitKeys(stageCatalog);        // "Portrait/<��������>"
        var illustKeys = CollectCharacterIllustKeys(characterCatalog); // "Illust/<ĳ����>"
        var stageIllustKeys = CollectStageIllustKeys(stageCatalog);         // "StageIllust/<��������>"

        // Portrait�� ĳ����/���������� �����ؼ� �����ε�
        var allPortraitKeys = new HashSet<string>(charPortraitKeys);
        foreach (var k in stagePortraitKeys) allPortraitKeys.Add(k);

        // Illustration�� ĳ����/���������� ����
        var allIllustKeys = new HashSet<string>(illustKeys);
        foreach (var k in stageIllustKeys) allIllustKeys.Add(k);

        // 2) ���� �����ε�
        var tasks = new List<Task>(5);

        // �ִϸ��̼� Ŭ��
        if (animKeys.Count > 0)
            tasks.Add(AnimationClipLibrary.Instance.LoadAssetsAsync(animKeys));
        else
            _ = AnimationClipLibrary.Instance.LoadAssetsAsync(new List<string>()); // no-op �����

        // ĳ����/�������� ��ü
        tasks.Add(StageLibrary.Instance.PreloadAsync(stageKeys));
        tasks.Add(CharacterLibrary.Instance.PreloadAsync(charKeys));

        // �ʻ� & �Ϸ���Ʈ
        tasks.Add(PortraitLibrary.Instance.PreloadAsync(allPortraitKeys.ToList()));
        tasks.Add(IllustrationLibrary.Instance.PreloadAsync(allIllustKeys.ToList()));

        // �� ���̺귯�� ����� ������
        var progressReaders = new System.Func<float>[]
        {
            () => AnimationClipLibrary.Instance.LoadProgress,
            () => StageLibrary.Instance.LoadProgress,
            () => CharacterLibrary.Instance.LoadProgress,
            () => PortraitLibrary.Instance.LoadProgress,
            () => IllustrationLibrary.Instance.LoadProgress,
        };

        // 3) ���� �� ������Ʈ ����
        while (tasks.Any(t => !t.IsCompleted))
        {
            // ��� �����
            float sum = 0f;
            int count = 0;
            for (int i = 0; i < progressReaders.Length; i++)
            {
                sum += Mathf.Clamp01(progressReaders[i]());
                count++;
            }
            Progress = (count > 0) ? (sum / count) : 0f;
            UpdateUI();
            await Task.Yield();
        }

        // Ȥ�� ���� �߻������� throw (����� �� ���� �ľ�)
        await Task.WhenAll(tasks);

        // 4) �Ϸ� ó��
        Progress = 1f;
        UpdateUI();
        IsPreloading = false;
        IsReady = true;
        onPreloadCompleted?.Invoke();

        if (loadingPanel) loadingPanel.SetActive(false);
    }

    void UpdateUI()
    {
        if (progressBar) progressBar.value = Progress;
    }

    // --- Collectors ---

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

    static List<string> CollectCharacterPortraitKeys(CharacterCatalog_SO cc)
    {
        var set = new HashSet<string>();
        if (cc?.entries == null) return set.ToList();
        foreach (var e in cc.entries)
            if (!string.IsNullOrEmpty(e.characterName))
                set.Add($"Portrait/{e.characterName}");
        return set.ToList();
    }

    static List<string> CollectStagePortraitKeys(StageCatalog_SO sc)
    {
        var set = new HashSet<string>();
        if (sc?.entries == null) return set.ToList();
        foreach (var e in sc.entries)
            if (!string.IsNullOrEmpty(e.stageName))
                set.Add($"Portrait/{e.stageName}");
        return set.ToList();
    }

    static List<string> CollectCharacterIllustKeys(CharacterCatalog_SO cc)
    {
        var set = new HashSet<string>();
        if (cc?.entries == null) return set.ToList();
        foreach (var e in cc.entries)
            if (!string.IsNullOrEmpty(e.characterName))
                set.Add($"Illust/{e.characterName}");
        return set.ToList();
    }

    static List<string> CollectStageIllustKeys(StageCatalog_SO sc)
    {
        var set = new HashSet<string>();
        if (sc?.entries == null) return set.ToList();
        foreach (var e in sc.entries)
            if (!string.IsNullOrEmpty(e.stageName))
                set.Add($"StageIllust/{e.stageName}");
        return set.ToList();
    }
}
