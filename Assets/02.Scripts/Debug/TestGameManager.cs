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
        var keys = new List<string> { "TestIdle", "TestPunch", "TestFall",
        "TestJumpUp", "TestLand", "TestWalkB", "TestWalkF", "TestCrouch", "TestCrouchHit", "TestCrouchGuard",
        };
        var task = AnimationClipLibrary.Instance.LoadAssetsAsync(keys);

        while (!task.IsCompleted)
            yield return null;

        if (task.IsFaulted)
            Debug.LogError(task.Exception);
        // 완료 후 진행

        GameObject testMan = Instantiate(prefab_TestMan);
        GameObject testEnemy = Instantiate(prefab_TestMan);

        var boxes = testMan.GetComponents<BoxComponent>();

        foreach (var box in boxes)
        {
            BoxManager.Instance.Register(box);
        }

        var enemyBoxes = testEnemy.GetComponents<BoxComponent>();

        foreach (var box in enemyBoxes)
        {
            BoxManager.Instance.Register(box);
        }

        PhysicsManager.Instance.Register(testMan.GetComponent<PhysicsEntity>());
        PhysicsManager.Instance.Register(testEnemy.GetComponent<PhysicsEntity>());

        TickMaster.Instance.Register(BoxManager.Instance);
        TickMaster.Instance.Register(PhysicsManager.Instance);

        testMan.transform.position = new Vector3(-1, 0, 0);
        testMan.GetComponent<CharacterProperty>().SetFacing(true);

        testEnemy.transform.position = new Vector3(1, 0, 0);
        testEnemy.GetComponent<CharacterProperty>().SetFacing(false);
    }
}
