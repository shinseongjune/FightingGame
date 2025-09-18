// ForcedAnimationState.cs
using UnityEngine;

/// <summary>
/// ���� ����: �ܺο��� clipKey �����ؼ� 1ȸ ��� �� �߸� ����
/// </summary>
public class ForcedAnimationState : CharacterState
{
    string clipKey;

    public ForcedAnimationState(CharacterFSM f) : base(f) { }
    public override CharacterStateTag? StateTag => CharacterStateTag.ForcedAnimation;

    public void SetClip(string key) => clipKey = key;

    protected override void OnEnter()
    {
        // Ȧ������ clipKey �о����
        var holder = fsm.GetComponent<ForcedAnimParamHolder>();
        if (holder != null && !string.IsNullOrEmpty(holder.clipKey))
        {
            // ���� ���� ���� SetClip ����
            SetClip(holder.clipKey);
            // �Ҹ� �Ķ���Ͷ�� ��� ����ֱ�(����)
            holder.clipKey = null;
        }

        phys.mode = PhysicsMode.Kinematic;
        phys.isGravityOn = false;

        var k = string.IsNullOrEmpty(clipKey) ? animCfg.GetClipKey(AnimKey.PreBattle) : clipKey;
        if (!TryPlay(property.characterName + "/" + k, ReturnToNeutralPose))
        {
            // ���� �� �ٷ� ����
            ReturnToNeutralPose();
        }
    }

    protected override void OnTick() { }


    protected override void OnExit()
    {
        phys.mode = PhysicsMode.Normal;
        phys.isGravityOn = true;
        clipKey = null;
    }
}
