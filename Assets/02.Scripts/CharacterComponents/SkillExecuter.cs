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
        // TODO: ������, ���� ���� �� �߰�
    }

    private void ReturnToNeutralPose()
    {
        // TODO: ���¿� ���� "Idle", "AirIdle", "CrouchIdle" ������ ��ȯ
    }
}
