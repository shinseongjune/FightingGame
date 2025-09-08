using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public static class CatalogPreloader
{
    public static async Task PreloadAll(CharacterCatalog_SO charCatalog, StageCatalog_SO stageCatalog)
    {
        if (charCatalog != null)
        {
            // �ִϸ��̼� Ŭ��
            var clipKeys = charCatalog.characters
                .Where(c => c.animSet != null)
                .SelectMany(c => c.animSet.entries.Select(e => e.clipKey))
                .Distinct()
                .ToList();
            await AnimationClipLibrary.Instance.LoadAssetsAsync(clipKeys);

            // Portrait & Illust
            await PortraitLibrary.Instance.LoadAssetsAsync(CatalogIds.AllPortraitKeys(charCatalog).ToList());
            await IllustrationLibrary.Instance.LoadAssetsAsync(CatalogIds.AllIllustrationKeys(charCatalog).ToList());
        }

        if (stageCatalog != null)
        {
            // Stage prefab �ٿ�ε常 (�ʿ�� Load��Release�� warm-up ����)
            foreach (var k in CatalogIds.AllStageKeys(stageCatalog))
            {
                var dd = Addressables.DownloadDependenciesAsync(k);
                await dd.Task;
                Addressables.Release(dd);
            }
        }
    }
}
