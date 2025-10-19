using UnityEngine;

[RequireComponent(typeof(CharacterFSM))]
public class CharacterRootTicker : MonoBehaviour, ITicker
{
    CharacterFSM fsm;
    InputBuffer input;
    AnimationPlayer anim;
    BoxPresetApplier boxApplier;
    CollisionResolver resolver;

    private TickMaster tickMaster;

    void Awake()
    {
        fsm = GetComponent<CharacterFSM>();
        input = GetComponent<InputBuffer>();
        anim = GetComponent<AnimationPlayer>();
        boxApplier = GetComponent<BoxPresetApplier>();
        resolver = GetComponent<CollisionResolver>();
    }

    void OnEnable()
    {
        tickMaster = TickMaster.Instance;
        if (tickMaster != null)
            tickMaster.Register(this);
    }

    void OnDisable()
    {
        if (tickMaster != null)
            tickMaster.Unregister(this);

        tickMaster = null;
    }

    // 한 틱의 호출 순서를 ‘여기서’ 보장
    public void Tick()
    {
        // 0) (선택) 물리 선적용이 캐릭터 내부에 필요하면 여기서
        // phys?.PreTick(); // 쓰고 있으면

        // 1) 입력 수집
        input?.Tick();

        // 2) FSM(상태.OnTick) : 커맨드 인식/상태 전이/플래그 업데이트
        fsm?.Tick();

        // 3) 애니메이션 프레임 전진(Playables 기반)
        anim?.Tick();

        // 4) 스킬/박스 스케줄 처리 (현재 프레임의 생성/소멸)
        boxApplier?.Tick();

        // 5) 충돌 처리(본인 관점 우선순위 필터링 등)
        resolver?.Tick();

        TryAutoFaceWhenBothGrounded();
    }

    void TryAutoFaceWhenBothGrounded()
    {
        var me = GetComponent<CharacterProperty>();
        if (me == null) return;
        var myPhys = GetComponent<PhysicsEntity>();
        if (myPhys == null) return;

        // 상대 찾기(2인 가정)
        CharacterProperty enemy = null;
        foreach (var c in GameObject.FindObjectsByType<CharacterProperty>(FindObjectsSortMode.None))
            if (c != me) { enemy = c; break; }
        if (enemy == null) return;

        var enPhys = enemy.GetComponent<PhysicsEntity>();
        if (enPhys == null) return;

        // 1) 양측 모두 지상일 때만
        if (!(myPhys.isGrounded && enPhys.isGrounded)) return;

        // 3) 히스테리시스(동일/근접 X에서 좌우 튀는 것 방지)
        const float swapThreshold = 0.05f; // 스테이지 유닛 기준, 필요시 0.1~0.2
        float dx = enPhys.Position.x - myPhys.Position.x;
        if (Mathf.Abs(dx) < swapThreshold) return;

        bool meShouldFaceRight = dx > 0f;

        me.SetFacing(meShouldFaceRight);
    }
}
