using System.Collections;
using UnityEngine;

public class AddressableStageLoader : MonoBehaviour, IStageLoader
{
    [SerializeField] private Transform stageRoot;

    public IEnumerator LoadAsync(string stageId)
    {
        var t = StageLibrary.Instance.LoadAsync(stageId, stageRoot);
        while (!t.IsCompleted) yield return null;

        if (!t.Result)
            Debug.LogError($"[StageLoader] Load failed: {stageId}");
    }

    public IEnumerator UnloadAsync()
    {
        var t = StageLibrary.Instance.UnloadAsync();
        while (!t.IsCompleted) yield return null;
    }
}