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

    // �ܺο��� �� �ٷ� ȣ��: �긮�� �� Ȧ���� Ű ���� �� �������·� ��ȯ
    public void PlayOnce(string clipKey)
    {
        holder.Set(clipKey);
        fsm.ForceSetState("ForcedAnimation");
    }
}

// ForcedAnimationState�� Enter()���� �� ���� �о���� ���� ����Ȧ��
public class ForcedAnimParamHolder : MonoBehaviour
{
    public string clipKey;
    public void Set(string key) => clipKey = key;
}