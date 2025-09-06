using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterFSM))]
[RequireComponent(typeof(PhysicsEntity))]
[RequireComponent(typeof(CharacterProperty))]
public class DebugAutoDriver : MonoBehaviour, ITicker
{
    [Header("Wiring")]
    public BattleManager gm;                 // 비워두면 자동 찾음
    public Skill_SO skillToUse;            // 주기적으로 쓸 스킬(옵션)

    [Header("Move Pattern")]
    public float moveSpeed = 2.0f;         // 좌우 이동 속도(유닛/초)
    public int moveFrames = 30;            // 이동 유지 프레임 수
    public int waitFrames = 20;            // 정지 대기 프레임 수

    [Header("Attack Pattern")]
    public int attackEveryNFrames = 60;    // N프레임마다 스킬 사용(0이면 사용 안함)

    CharacterFSM fsm;
    PhysicsEntity phys;
    CharacterProperty prop;

    int frame;    // 전체 프레임 누적
    int phase;    // 0,1,0,1… 짝수=→, 홀수=←
    int local;    // 현 단계에서 경과 프레임

    void Awake()
    {
        fsm = GetComponent<CharacterFSM>();
        phys = GetComponent<PhysicsEntity>();
        prop = GetComponent<CharacterProperty>();
        if (gm == null) gm = FindFirstObjectByType<BattleManager>();
    }

    void OnEnable() => TickMaster.Instance?.Register(this);
    void OnDisable() => TickMaster.Instance?.Unregister(this);

    public void Tick()
    {
        // 가드레일: 게임 흐름/입력 잠금/디버그 플래그
        if (gm == null || !gm.enabled || !gameObject.activeInHierarchy) return;
        if (!gm.debugAuto) return;
        if (prop != null && !prop.isInputEnabled) return;

        frame++;
        local++;

        // ----- 좌우 왕복 패턴 -----
        int dir = (phase % 2 == 0) ? +1 : -1; // 짝수=오른쪽, 홀수=왼쪽
        if (local <= moveFrames)
        {
            float dx = dir * moveSpeed * TickMaster.TICK_INTERVAL;
            phys.Position += new Vector2(dx, 0f);
            phys.SyncTransform(); // 즉시 반영(렌더/박스 일치)
        }
        else if (local > moveFrames + waitFrames)
        {
            local = 0;
            phase++;
            // 바라보는 방향 갱신(선택): 이동 방향 기준
            //if (prop != null) prop.isFacingRight = (dir > 0);
        }

        // ----- 주기적 공격 -----
        if (attackEveryNFrames > 0 && skillToUse != null && (frame % attackEveryNFrames) == 0)
        {
            prop.currentSkill = skillToUse;
            fsm.TransitionTo("Skill");
        }
    }
}
