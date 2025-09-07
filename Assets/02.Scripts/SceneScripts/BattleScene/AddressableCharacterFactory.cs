using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class AddressableCharacterFactory : MonoBehaviour, ICharacterFactory
{
    public CoroutineWithResult<CharacterProperty> SpawnAsync(PlayerLoadout loadout, Vector2 worldPos, bool facingRight)
    {
        var cr = new CoroutineWithResult<CharacterProperty>();
        StartCoroutine(SpawnRoutine(loadout, worldPos, facingRight, cr));
        return cr;
    }

    private IEnumerator SpawnRoutine(PlayerLoadout loadout, Vector2 worldPos, bool facingRight, CoroutineWithResult<CharacterProperty> cr)
    {
        // 1) ������ �ε�
        var task = CharacterLibrary.Instance.LoadPrefabAsync(loadout.characterId);
        while (!task.IsCompleted) yield return null;

        var prefab = task.Result;
        if (prefab == null) { cr.SetResult(null); yield break; }

        // 2) �ν��Ͻ� ����
        var go = Instantiate(prefab);
        var prop = go.GetComponent<CharacterProperty>();
        if (prop == null) { Debug.LogError("[CharacterFactory] CharacterProperty missing on prefab"); cr.SetResult(null); yield break; }

        // 3) ��ġ/����
        prop.SpawnAt(worldPos, facingRight);

        // 4) (����) �ִϼ�/��ų/�ڽ� ���ε�: īŻ�α�/���̺��� ����Ѵٸ� ���⼭
        // ex) var catalog = CharacterCatalog.Instance.Resolve(loadout.characterId); prop.allSkills = catalog.skills;

        cr.SetResult(prop);
    }
}
