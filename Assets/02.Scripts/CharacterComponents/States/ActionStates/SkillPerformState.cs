using UnityEngine;

public class SkillPerformState : CharacterState
{
    private int _frame;
    private Skill_SO skill;
    private bool finished;

    private bool wasGrounded;

    public bool isRushCanceled;

    // ������ ��: Ư�� �����Ӻ��� ĵ�� ���
    // �ʿ��ϸ� Skill_SO�� â(window) �����͸� �־�͵� ��.
    private int cancelStartFrame = 0;   // ��: 0�̸� ��� �Ұ�
    private int cancelEndFrame = 999; // ����� ũ��

    private int _lastSpawnCheckedFrame = -1;

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
        _frame = 0;

        if (property.isRushCanceled)
        {
            isRushCanceled = true;
            property.isRushCanceled = false;
        }

        property.attackInstanceId = CharacterProperty.NextAttackInstanceId();

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

        property.isSkillCancelable = false;
        property.isDriveGaugeCharging = false;

        wasGrounded = phys.isGrounded;

        TryPlayStartVfx(skill);
    }

    protected override void OnTick()
    {
        if (!wasGrounded && phys.isGrounded)
        {
            fsm.TransitionTo("Idle");
        }

        wasGrounded = phys.isGrounded;

        // ĵ�� Ÿ�̹� ����(������ ��� ����)
        int f = anim.CurrentClipFrame;
        property.isSkillCancelable = (f >= cancelStartFrame && f <= cancelEndFrame);

        // ����ü ���� ó��(�� �����ӿ� ���� ���� ó��)
        if (skill != null && skill.spawnsProjectiles && skill.projectileSpawns != null)
        {
            for (int i = 0; i < skill.projectileSpawns.Length; ++i)
            {
                var ev = skill.projectileSpawns[i];
                if (ev.frame > _lastSpawnCheckedFrame && ev.frame <= f)
                {
                    SpawnProjectile(ev);
                }
            }
            _lastSpawnCheckedFrame = f;
        }

        // ��Ʈĵ�� or �Ϲ� ĵ�� �Է�
        if (!finished && property.isSkillCancelable)
        {
            if (TryStartSkill())     // �Է� �νĵǸ� ���� ��ų�� ĵ��
                return;
        }

        ProcessSkillFxCues(skill, _frame, fsm.transform, property.ResolveBoneTransform);
        _frame++;
    }

    protected override void OnExit()
    {
        // ���� �������� �� �Է� ��� ����
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

    private void SpawnProjectile(ProjectileSpawnEvent ev)
    {
        if (ev.prefab == null) return;

        // ���� ã��
        Transform spawnTr = null;
        var sockets = go.GetComponent<CharacterSockets>();
        if (sockets != null && !string.IsNullOrEmpty(ev.socketName))
            spawnTr = sockets.Find(ev.socketName);

        Vector3 basePos = (spawnTr != null ? spawnTr.position : tr.position);
        Quaternion rot = Quaternion.identity;

        var projGo = Object.Instantiate(ev.prefab, basePos + ev.localOffset, rot);
        var proj = projGo.GetComponent<ProjectileController>();
        if (proj == null)
        {
            Debug.LogWarning("[Projectile] Prefab�� ProjectileController�� �ʿ��մϴ�.");
            Object.Destroy(projGo);
            return;
        }

        // �ʱ�ȭ
        proj.Init(property, ev, property.isFacingRight, skill);

        // (����) ������Ʈ ����
        if (property.projectiles != null)
            property.projectiles.Add(projGo);
    }
}
