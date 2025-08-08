using UnityEngine;

public class SkillState : CharacterState
{
    private CharacterProperty property;
    private AnimationPlayer animator;

    private Skill_SO currentSkill;

    public SkillState(CharacterFSM fsm) : base(fsm)
    {
        this.fsm = fsm;
        this.owner = fsm.gameObject;
        this.property = owner.GetComponent<CharacterProperty>();
        this.animator = owner.GetComponent<AnimationPlayer>();
    }

    public override void OnEnter()
    {
        if (currentSkill == null)
        {
            Debug.LogError("[SkillState] currentSkill is null.");
            return;
        }


        animator.Play(currentSkill.animationClipName, OnSkillFinished);
    }

    private void OnSkillFinished()
    {
    }

    public override void OnUpdate()
    {
        // 스킬 중에는 입력 무시
    }

    public override void OnExit()
    {
    }
}
