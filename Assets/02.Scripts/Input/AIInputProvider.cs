using UnityEngine;

/// <summary>
/// 간단 유틸리티 + 랜덤 기반의 AI 입력 공급자.
/// - AntiAir: 상대가 공중 & 낙하 중 & 수평거리 근접 시, (대공타 공격 or 앞점프-공중공격) 중 랜덤 선택
/// - Approach: 평소에는 전진
/// - KeepOut: 너무 붙으면 살짝 뒤로
/// 
/// 필요한 값은 CharacterProperty/Transform에서 읽고,
/// 최종적으로 InputData를 1틱 단위로 만들어 반환한다.
/// </summary>
public sealed class AIInputProvider : IInputProvider
{
    private readonly CharacterProperty me;
    private readonly CharacterProperty enemy;

    // === 튜닝 파라미터 ===
    public float antiAirRangeX = 2.2f;     // 대공 판단 수평거리
    public float antiAirFallVy = -0.05f;   // '낙하중' 판단 임계치
    public float keepOutRangeX = 1.2f;     // 이 거리보다 붙었으면 뒤로 살짝
    public float approachRangeX = 3.5f;    // 이 거리보다 멀면 전진

    [Range(0f, 1f)] public float antiAirSkillProb = 0.6f; // 대공 시 '지상 대공타' 선택 확률
    public AttackKey antiAirGroundKey = AttackKey.HP;     // 대공 지상타로 누를 키(플래그 가능)
    public AttackKey airAttackKey = AttackKey.MP;     // 점프 후 공중 공격으로 누를 키

    public int rngSeed = 1234567;

    private System.Random rng;

    public AIInputProvider(CharacterProperty owner, CharacterProperty other)
    {
        me = owner;
        enemy = other;
        // 결정성(리플레이/테스트 재현)을 위해 고정 시드. 필요시 매치 시드 XOR 등으로 바꿔도 됨.
        rng = new System.Random(rngSeed ^ (owner != null ? owner.GetInstanceID() : 9973));
    }

    public InputData GetSnapshot()
    {
        var tick = TickMaster.Instance != null ? TickMaster.Instance.CurrentTick : 0;
        var b = new InputDataBuilder(me); // me의 facing 기준으로 방향 매핑

        // === 관측 ===
        Vector2 mePos = me != null ? (Vector2)me.transform.position : Vector2.zero;
        Vector2 enemyPos = enemy != null ? (Vector2)enemy.transform.position : Vector2.zero;
        Vector2 enemyVel = Vector2.zero;
        if (enemy != null && enemy.phys != null)
            enemyVel = enemy.phys.Velocity; // 없다면 0으로

        float dx = Mathf.Abs(enemyPos.x - mePos.x);
        float dy = enemyPos.y - mePos.y;

        bool enemyInAir = enemy != null && !enemy.phys.isGrounded; // 없으면 transform.y로 간단 판단
        if (enemy != null)
        {
            // 간이: 지면 높이 0 기준 (프로젝트에 맞게 바꾸세요)
            enemyInAir = enemyPos.y > 0.05f;
        }

        // === 간단 정책 ===
        // 1) 대공 판단
        if (enemyInAir && enemyVel.y <= antiAirFallVy && dx <= antiAirRangeX)
        {
            if (rng.NextDouble() < antiAirSkillProb)
            {
                // (1) 지상 대공타: 제자리 공격
                b.Neutral();                // 방향 중립
                b.Press(antiAirGroundKey);  // 대공용 키
            }
            else
            {
                // (2) 앞점프 → 공중공격
                b.Forward(); // 앞점프 유도: 이 틱에서 '앞' 입력
                b.Jump();    // 점프(실제 Jump는 AttackKey에 없으므로, 보통 점프는 방향=Up/UpForward로 표현)
                // 다음 틱부터가 아니라 즉시 한 프레임에 몰아넣으면 입력 인식이 어려울 수 있으니
                // 이 Provider는 '한 틱 스냅샷'만 반환한다는 점을 고려해, 점프 프레임에는 Up/UpForward를,
                // 공격은 같은 틱에 눌러도 되도록 단순화한다.
                b.Press(airAttackKey);
            }

            return b.Build(tick);
        }

        // 2) 간단 거리 기반 어프로치/킵아웃
        if (dx >= approachRangeX)
        {
            // 멀다 → 전진
            b.Forward();
        }
        else if (dx <= keepOutRangeX)
        {
            // 너무 가깝다 → 살짝 뒤로
            // 랜덤하게 가끔은 미들/라이트 포킹
            b.Back();
            if (rng.NextDouble() < 0.15)
                b.Press(AttackKey.MK);
        }
        else
        {
            // 적당한 거리 → 가끔 찌르기
            b.Neutral();
            if (rng.NextDouble() < 0.25)
                b.Press(AttackKey.MP);
        }

        return b.Build(tick);
    }
}