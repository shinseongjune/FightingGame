using UnityEngine;

public class BattleManager : MonoBehaviour
{
    [SerializeField] private Transform stageRoot;
    [SerializeField] private Transform p1Spawn;
    [SerializeField] private Transform p2Spawn;

    GameObject stageGO, p1GO, p2GO;

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

        var walls = stageGO.GetComponents<BoxComponent>();
        foreach (var wall in walls)
        {

        }
    }
}
