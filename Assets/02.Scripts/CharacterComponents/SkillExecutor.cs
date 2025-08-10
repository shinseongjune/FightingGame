using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterProperty), typeof(InputBuffer), typeof(AnimationPlayer))]
public class SkillExecutor : MonoBehaviour, ITicker
{
    public Skill_SO currentSkill;

    private CharacterProperty property;
    private InputBuffer buffer;
    private AnimationPlayer animator;
    private BoxPresetApplier boxApplier;

    [SerializeField] private List<Skill_SO> allSkills;

    private void Awake()
    {
        property = GetComponent<CharacterProperty>();
        buffer = GetComponent<InputBuffer>();
        animator = GetComponent<AnimationPlayer>();
        boxApplier = GetComponent<BoxPresetApplier>();
    }

    public void Tick()
    {
        List<Skill_SO> usableSkills = new List<Skill_SO>();

        foreach (Skill_SO skill in allSkills)
        {
            if ((skill.condition.currentSkill == null || skill.condition.currentSkill == currentSkill)
                && (skill.condition.currentCharacterState == CharacterStateTag.None || skill.condition.currentCharacterState == property.characterStateTag))
            {
                usableSkills.Add(skill);
            }
        }

        Skill_SO matched = InputRecognizer.Recognize(buffer.inputQueue, usableSkills);
        if (matched != null)
        {
            PlaySkill(matched);
        }
    }

    private void PlaySkill(Skill_SO skill)
    {
        currentSkill = skill;
        boxApplier.ApplySkill(skill);

        animator.Play(skill.animationClipName, ReturnToNeutralPose);
    }

    private void ReturnToNeutralPose()
    {
        //TODO: currentskill 초기화, 자세 초기화, 박스 변경
    }
}
