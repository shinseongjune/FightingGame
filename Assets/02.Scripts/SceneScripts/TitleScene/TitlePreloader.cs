using UnityEngine;
using UnityEngine.AddressableAssets;

public class TitlePreloader : UnityEngine.MonoBehaviour
{
    public CharacterCatalog_SO characterCatalog;
    public StageCatalog_SO stageCatalog;
    public string nextSceneAddress = "SelectScene";
    [SerializeField] GameObject loadingPanel;

    private async void Start()
    {
        loadingPanel.SetActive(true);

        await CatalogPreloader.PreloadAll(characterCatalog, stageCatalog);
        await Addressables.LoadSceneAsync(nextSceneAddress).Task;

        loadingPanel.SetActive(false);
    }
}
