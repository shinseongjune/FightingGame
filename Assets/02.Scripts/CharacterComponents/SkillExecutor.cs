using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

[RequireComponent(typeof(CharacterProperty), typeof(InputBuffer), typeof(AnimationPlayer))]
public class SkillExecutor : MonoBehaviour, ITicker
{
    public Skill_SO currentSkill;

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
        Skill_SO matched = InputRecognizer.Recognize(buffer.inputQueue, property.usableSkills);
        if (matched != null)
        {
            PlaySkill(matched);
        }
    }

    private void PlaySkill(Skill_SO skill)
    {
        currentSkill = skill;

        animator.Play(skill.animationClipName, ReturnToNeutralPose);
        // TODO: 데미지, 상태 설정 등 추가
    }

    private void ReturnToNeutralPose()
    {

    }
}
