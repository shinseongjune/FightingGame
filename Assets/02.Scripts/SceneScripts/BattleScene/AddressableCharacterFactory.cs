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
        // 1) 프리팹 로드
        var task = CharacterLibrary.Instance.LoadPrefabAsync(loadout.characterId);
        while (!task.IsCompleted) yield return null;

        var prefab = task.Result;
        if (prefab == null) { cr.SetResult(null); yield break; }

        // 2) 인스턴스 생성
        var go = Instantiate(prefab);
        var prop = go.GetComponent<CharacterProperty>();
        if (prop == null) { Debug.LogError("[CharacterFactory] CharacterProperty missing on prefab"); cr.SetResult(null); yield break; }

        // 3) 위치/방향
        prop.SpawnAt(worldPos, facingRight);

        // 4) (선택) 애니셋/스킬/박스 바인딩: 카탈로그/테이블을 사용한다면 여기서
        // ex) var catalog = CharacterCatalog.Instance.Resolve(loadout.characterId); prop.allSkills = catalog.skills;

        cr.SetResult(prop);
    }
}
