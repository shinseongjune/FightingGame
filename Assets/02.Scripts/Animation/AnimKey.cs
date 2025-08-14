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
    ThrowStart,     // ��� ����(������)
    ThrowLoop,      // ���� ä�� ����
    ThrowEnd,       // ������ ���(������)
    BeingThrown,    // ����/������(�ǰ���)
    Forced,          // ���� ����
    // ����: DashF, DashB, Guard, Knockdown ��
}