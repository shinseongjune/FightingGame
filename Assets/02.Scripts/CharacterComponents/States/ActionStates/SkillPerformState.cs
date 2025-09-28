public class SkillPerformState : CharacterState
{
    private Skill_SO skill;
    private bool finished;

    private bool wasGrounded;

    public bool isRushCanceled;

    // ������ ��: Ư�� �����Ӻ��� ĵ�� ���
    // �ʿ��ϸ� Skill_SO�� â(window) �����͸� �־�͵� ��.
    private int cancelStartFrame = 0;   // ��: 0�̸� ��� �Ұ�
    private int cancelEndFrame = 999; // ����� ũ��

    public SkillPerformState(CharacterFSM f) : base(f) { }

    public override CharacterStateTag? StateTag
    {
        get
        {
            if (isRushCanceled)
                return CharacterStateTag.DriveRushSkill;

            return CharacterStateTag.Skill;
        }
    }

    protected override void OnEnter()
    {
        if (property.isRushCanceled)
        {
            isRushCanceled = true;
            property.isRushCanceled = false;
        }

        property.attackInstanceId++;

        finished = false;

        skill = property.currentSkill;
        if (skill == null) { Transition("Idle"); return; }

        phys.mode = PhysicsMode.Normal;
        phys.isGravityOn = true;
        phys.SetActiveWhiffBoxes(true);

        bool ok = TryPlay(property.characterName + "/" + skill.animationClipName, OnAnimComplete);
        if (!ok)
        {
            UnityEngine.Debug.LogWarning($"[Skill] Animation not found for '{skill.skillName}' : '{skill.animationClipName}'");
            boxApplier.ClearAllBoxes();
            ReturnToNeutral();
            return;
        }

        property.isInputEnabled = false;
        property.isSkillCancelable = false;
        property.isDriveGaugeCharging = false;

        wasGrounded = phys.isGrounded;
    }

    protected override void OnTick()
    {
        if (!wasGrounded && phys.isGrounded)
        {
            fsm.TransitionTo("Idle");
        }

        wasGrounded = phys.isGrounded;

        // 1) ĵ�� Ÿ�̹� ����(������ ��� ����)
        int f = anim.CurrentClipFrame;
        property.isSkillCancelable = (f >= cancelStartFrame && f <= cancelEndFrame);

        // 2) ��Ʈĵ�� or �Ϲ� ĵ�� �Է�
        if (!finished && property.isSkillCancelable)
        {
            if (TryStartSkill())     // �Է� �νĵǸ� ���� ��ų�� ĵ��
                return;
        }
    }

    protected override void OnExit()
    {
        // ���� �������� �� �Է� ��� ����
        property.isInputEnabled = true;
        property.isSkillCancelable = false;
        property.currentSkill = null;
        property.isDriveGaugeCharging = true;

        phys.SetActiveWhiffBoxes(false);

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
        property.ChargeDriveGauge(skill.driveGaugeChargeAmount);
    }

    public override void HandleGuard(PhysicsEntity atk, PhysicsEntity def, CollisionData cd)
    {
        // ����� ���� ĵ�� �� ��Ģ�� ������ ���⼭
        property.isSkillCancelable = true;
    }

    public override void HandleThrow(PhysicsEntity atk, PhysicsEntity def, CollisionData cd)
    {

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
