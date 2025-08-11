public class SkillPerformState : CharacterState
{
    private Skill_SO skill;
    private bool finished;

    // ������ ��: Ư�� �����Ӻ��� ĵ�� ���
    // �ʿ��ϸ� Skill_SO�� â(window) �����͸� �־�͵� ��.
    private int cancelStartFrame = 0;   // ��: 0�̸� ��� �Ұ�
    private int cancelEndFrame = 999; // ����� ũ��

    public SkillPerformState(CharacterFSM f) : base(f) { }

    public override CharacterStateTag? StateTag => CharacterStateTag.Skill;

    protected override void OnEnter()
    {
        finished = false;

        // ���� ������ TryStartSkill���� �ھƵ� ��ų ȹ��
        skill = property.currentSkill;
        if (skill == null)
        {
            Transition("Idle");
            return;
        }

        // ����/���� ���(���� ��� ����)
        phys.mode = PhysicsMode.Normal;
        phys.isGravityOn = true;
        phys.SetPose(CharacterStateTag.Skill);

        // �ڽ�/�ִ� ����
        boxApplier.ApplySkill(skill);
        anim.Play(skill.animationClipName, OnAnimComplete);

        // ���� �� �Է� ���/ĵ�� ���� ���� �� �ʿ� �÷���
        property.isInputEnabled = false;
        property.isSkillCancelable = false;
    }

    protected override void OnTick()
    {
        // 1) ĵ�� Ÿ�̹� ����(������ ��� ����)
        int f = anim.CurrentFrame;
        property.isSkillCancelable = (f >= cancelStartFrame && f <= cancelEndFrame);

        // 2) ��Ʈĵ�� or �Ϲ� ĵ�� �Է�
        if (!finished && property.isSkillCancelable)
        {
            if (TryStartSkill())     // �Է� �νĵǸ� ���� ��ų�� ĵ��
                return;
        }

        // 3) ���� �� ��� �߰��� �����ص� ������ ������� �ʰ�,
        //    �ִ� ������ ReturnToNeutral�� �¿�. �ʿ��ϸ� ���⼭ ���� ���� ó���� �߰�.
    }

    protected override void OnExit()
    {
        // ���� �������� �� �Է� ��� ����
        property.isInputEnabled = true;
        property.isSkillCancelable = false;

        // FSM�� �⺻������ ���� �� BoxPresetApplier.ClearAll()�� ȣ���ϰ� ������
        // ���⼭ ���� �ڽ��� ���� �ʿ�� ����.
    }

    private void OnAnimComplete()
    {
        finished = true;
        ReturnToNeutral();
    }

    // ---- �浹 �̺�Ʈ(��Ʈĵ��/����ĵ�� ��) ----
    public override void HandleHit(HitData hit)
    {
        // ��Ʈ Ȯ�� �� ��� ĵ�� â ����(���ϸ� ���Ǻ�)
        property.isSkillCancelable = true;
    }

    public override void HandleGuard(PhysicsEntity atk, PhysicsEntity def, CollisionData cd)
    {
        // ����� ���� ĵ�� �� ��Ģ�� ������ ���⼭
        property.isSkillCancelable = true;
    }

    public override void HandleThrow(PhysicsEntity atk, PhysicsEntity def, CollisionData cd)
    {
        // ��� ���� �ÿ��� ���� ���� ����
        fsm.TransitionTo("Throw"); // �����Ǿ� �ִٸ�
    }

    // ---- �߸� ���� ----
    private void ReturnToNeutral()
    {
        // �����̸� ����, �ɱ� ������ �ɱ�, �� �� ���
        var d = input?.LastInput.direction ?? Direction.Neutral;

        if (!phys.isGrounded) { Transition("Fall"); return; }

        if (d is Direction.Down or Direction.DownBack or Direction.DownForward)
        { Transition("Crouch"); return; }

        Transition("Idle");
    }
}
