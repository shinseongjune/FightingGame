public enum AnimKey
{
    Idle, Crouch,
    WalkF, WalkB,
    JumpUp, JumpF, JumpB, Fall, Land,

    GuardIdle,      // 가드 유지 포즈
    GuardHit,       // 가드 경직
    HitGround,      // 지상 피격
    HitAir,         // 공중 피격(이미 쓰고 있으면 유지)
    Knockdown,
    HardKnockdown,
    WakeUp,         // 기상
    ThrowStart,     // 잡기 시작(시전자)
    ThrowLoop,      // 잡은 채로 고정(선택)
    ThrowEnd,       // 던지기 모션(시전자)
    BeingThrown,    // 잡힘/던져짐(피격자)
    Forced          // 연출 전용
    // 추후: DashF, DashB, Guard, Knockdown 등
}