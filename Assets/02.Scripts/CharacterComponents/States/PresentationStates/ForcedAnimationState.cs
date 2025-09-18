// ForcedAnimationState.cs
using UnityEngine;

/// <summary>
/// 연출 전용: 외부에서 clipKey 지정해서 1회 재생 후 중립 복귀
/// </summary>
public class ForcedAnimationState : CharacterState
{
    string clipKey;

    public ForcedAnimationState(CharacterFSM f) : base(f) { }
    public override CharacterStateTag? StateTag => CharacterStateTag.ForcedAnimation;

    public void SetClip(string key) => clipKey = key;

    protected override void OnEnter()
    {
        // 홀더에서 clipKey 읽어오기
        var holder = fsm.GetComponent<ForcedAnimParamHolder>();
        if (holder != null && !string.IsNullOrEmpty(holder.clipKey))
        {
            // 상태 시작 전에 SetClip 보장
            SetClip(holder.clipKey);
            // 소모성 파라미터라면 즉시 비워주기(선택)
            holder.clipKey = null;
        }

        phys.mode = PhysicsMode.Kinematic;
        phys.isGravityOn = false;

        var k = string.IsNullOrEmpty(clipKey) ? animCfg.GetClipKey(AnimKey.PreBattle) : clipKey;
        if (!TryPlay(property.characterName + "/" + k, ReturnToNeutralPose))
        {
            // 실패 시 바로 복귀
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
