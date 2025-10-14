public enum AnimKey
{
    Idle, Crouch,
    WalkF, WalkB,
    JumpUp, JumpF, JumpB, Fall, Land,

    GuardIdle,      // ���� ���� ����
    GuardCrouch,    // �ɾ� ����
    GuardHit,       // ���� ����
    GuardHitCrouch, // �ɾ� ���� ����
    HitGround,      // ���� �ǰ�
    HitCrouch,      // �ɾ� �ǰ�
    HitAir,         // ���� �ǰ�
    Knockdown,
    HardKnockdown,
    WakeUp,         // ���
    PreBattle,      // ���� ����
    Win,            // ���� ����
    Lose,
    DriveImpact,    // ����̺� ����Ʈ
    ParryStart, ParryLoop, ParryEnd, // ����̺� �и�
    // ����: DashF, DashB, Knockdown ��
}