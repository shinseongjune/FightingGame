using UnityEngine;

[RequireComponent(typeof(CharacterFSM))]
public class ForcedAnimationStateBridge : MonoBehaviour
{
    CharacterFSM fsm;
    ForcedAnimParamHolder holder;

    void Awake()
    {
        fsm = GetComponent<CharacterFSM>();
        holder = GetComponent<ForcedAnimParamHolder>();
        if (holder == null) holder = gameObject.AddComponent<ForcedAnimParamHolder>();
    }

    // 외부에서 한 줄로 호출: 브리지 → 홀더에 키 세팅 → 강제상태로 전환
    public void PlayOnce(string clipKey)
    {
        holder.Set(clipKey);
        fsm.ForceSetState("ForcedAnimation");
    }
}

// ForcedAnimationState가 Enter()에서 이 값을 읽어가도록 간단 공유홀더
public class ForcedAnimParamHolder : MonoBehaviour
{
    public string clipKey;
    public void Set(string key) => clipKey = key;
}