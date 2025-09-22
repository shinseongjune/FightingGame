using UnityEngine;

public class BattleManager : MonoBehaviour
{
    [SerializeField] private Transform stageRoot;
    [SerializeField] private Transform p1Spawn;
    [SerializeField] private Transform p2Spawn;
    [SerializeField] private RoundController roundController;

    GameObject stageGO, p1GO, p2GO;

    private void Awake()
    {
        Time.timeScale = 1f;
        GameManager.Instance.actions.Player.Enable();
        GameManager.Instance.actions.Select.Disable();
    }

    void Start()
    {
        var m = GameManager.Instance.matchConfig;
        string stageKey = $"Stage/{m.stageId}";
        string p1Key = $"Character/{m.p1.characterId}/{m.p1.costumeId}";
        string p2Key = $"Character/{m.p2.characterId}/{m.p2.costumeId}";

        stageGO = StageLibrary.Instance.Instantiate(stageKey, stageRoot);
        p1GO = CharacterLibrary.Instance.Instantiate(p1Key, p1Spawn.position, p1Spawn.rotation);
        p2GO = CharacterLibrary.Instance.Instantiate(p2Key, p2Spawn.position, p2Spawn.rotation);

        var p1Prop = p1GO.GetComponent<CharacterProperty>();
        var p2Prop = p2GO.GetComponent<CharacterProperty>();
        p1Prop?.SpawnAt(p1Spawn.position, true);
        p2Prop?.SpawnAt(p2Spawn.position, false);

        roundController.BindFighters(p1Prop, p2Prop);
        StartCoroutine(Co_BeginAfterReady());
    }

    private System.Collections.IEnumerator Co_BeginAfterReady()
    {
        yield return WaitFor.TickMasterReady();
        yield return WaitFor.PhysicsManagerReady();
        yield return WaitFor.BoxManagerReady();
        roundController.BeginMatch();
    }
}
