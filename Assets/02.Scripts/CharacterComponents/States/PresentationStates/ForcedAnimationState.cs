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
        phys.mode = PhysicsMode.Kinematic;
        phys.isGravityOn = false;

        var k = string.IsNullOrEmpty(clipKey) ? animCfg.GetClipKey(AnimKey.Forced) : clipKey;
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
