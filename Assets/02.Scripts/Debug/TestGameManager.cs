using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestGameManager : MonoBehaviour
{
    public GameObject prefab_TestMan;

    void Start()
    {
        StartCoroutine(LoadAnimClips());
    }

    IEnumerator LoadAnimClips()
    {
        var keys = new List<string> { "TestIdle", "TestPunch" };
        var task = AnimationClipLibrary.Instance.LoadAssetsAsync(keys);

        while (!task.IsCompleted)
            yield return null;

        if (task.IsFaulted)
            Debug.LogError(task.Exception);
        // �Ϸ� �� ����

        Instantiate(prefab_TestMan);
    }
}
