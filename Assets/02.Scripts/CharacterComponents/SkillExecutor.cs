using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterProperty), typeof(InputBuffer), typeof(AnimationPlayer))]
public class SkillExecutor : MonoBehaviour, ITicker
{
    public Skill_SO currentSkill;

    private CharacterProperty property;
    private InputBuffer buffer;
    private AnimationPlayer animator;

    [SerializeField] private List<Skill_SO> allSkills;

    private void Awake()
    {
        property = GetComponent<CharacterProperty>();
        buffer = GetComponent<InputBuffer>();
        animator = GetComponent<AnimationPlayer>();
    }

    public void Tick()
    {
        //TODO: 조건탐색->skills 전달
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
