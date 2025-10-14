public enum AnimKey
{
    Idle, Crouch,
    WalkF, WalkB,
    JumpUp, JumpF, JumpB, Fall, Land,

    GuardIdle,      // 가드 유지 포즈
    GuardCrouch,    // 앉아 가드
    GuardHit,       // 가드 경직
    GuardHitCrouch, // 앉아 가드 경직
    HitGround,      // 지상 피격
    HitCrouch,      // 앉아 피격
    HitAir,         // 공중 피격
    Knockdown,
    HardKnockdown,
    WakeUp,         // 기상
    PreBattle,      // 연출 전용
    Win,            // 연출 전용
    Lose,
    DriveImpact,    // 드라이브 임팩트
    ParryStart, ParryLoop, ParryEnd, // 드라이브 패리
    // 추후: DashF, DashB, Knockdown 등
}