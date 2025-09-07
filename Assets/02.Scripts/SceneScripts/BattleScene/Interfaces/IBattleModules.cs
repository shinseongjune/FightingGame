using System.Collections;
using UnityEngine;

public interface IStageLoader
{
    IEnumerator LoadAsync(string stageId);
    IEnumerator UnloadAsync();
}

public interface ICharacterFactory
{
    // facingRight: P1=true, P2=false (기본값)
    CoroutineWithResult<CharacterProperty> SpawnAsync(PlayerLoadout loadout, Vector2 worldPos, bool facingRight);
}

// 코루틴에서 값을 돌려주는 간단한 래퍼 (유틸)
public class CoroutineWithResult<T> : CustomYieldInstruction
{
    public T Result { get; private set; }
    public bool IsDone { get; private set; }
    public override bool keepWaiting => !IsDone;
    public void SetResult(T value) { Result = value; IsDone = true; }
}
