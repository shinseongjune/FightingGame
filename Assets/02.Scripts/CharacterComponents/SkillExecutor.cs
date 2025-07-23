using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

[RequireComponent(typeof(CharacterProperty), typeof(InputBuffer), typeof(AnimationPlayer))]
public class SkillExecutor : MonoBehaviour, ITicker
{
    private CharacterProperty property;
    private InputBuffer buffer;
    private AnimationPlayer animator;
    private PhysicsEntity entity;

    private void Awake()
    {
        property = GetComponent<CharacterProperty>();
        buffer = GetComponent<InputBuffer>();
        animator = GetComponent<AnimationPlayer>();
        entity = GetComponent<PhysicsEntity>();
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

        entity.RefreshBoxes();

        property.currentSkill = skill;
        property.usableSkills.Clear();
        property.usableSkills.AddRange(skill.nextSkills);
    }

    private void ReturnToNeutralPose()
    {
        string idleClip = property.isJumping ? "AirIdle"
                        : property.isSitting ? "CrouchIdle"
                        : "Idle";
        animator.Play(idleClip);

        property.currentSkill = null;
        property.usableSkills = property.isJumping ? property.jumpSkills
                              : property.isSitting ? property.crouchSkills
                              : property.idleSkills;
    }
}
