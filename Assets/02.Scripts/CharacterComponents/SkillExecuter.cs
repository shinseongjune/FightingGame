using UnityEngine;

[RequireComponent(typeof(CharacterProperty), typeof(InputBuffer), typeof(AnimationPlayer))]
public class SkillExecuter : MonoBehaviour, ITicker
{
    private CharacterProperty property;
    private InputBuffer buffer;
    private AnimationPlayer animator;

    private void Awake()
    {
        property = GetComponent<CharacterProperty>();
        buffer = GetComponent<InputBuffer>();
        animator = GetComponent<AnimationPlayer>();
    }

    public void Tick()
    {
        Skill matched = InputRecognizer.Recognize(buffer.inputQueue, property.usableSkills);
        if (matched != null)
        {
            PlaySkill(matched);
        }
    }

    private void PlaySkill(Skill skill)
    {
        animator.Play(skill.animationClipName, ReturnToNeutralPose);
        // TODO: 데미지, 상태 설정 등 추가
    }

    private void ReturnToNeutralPose()
    {
        // TODO: 상태에 따라 "Idle", "AirIdle", "CrouchIdle" 등으로 전환
    }
}
