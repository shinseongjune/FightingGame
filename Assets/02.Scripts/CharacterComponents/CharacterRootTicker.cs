using UnityEngine;

[RequireComponent(typeof(CharacterFSM))]
public class CharacterRootTicker : MonoBehaviour, ITicker
{
    CharacterFSM fsm;
    InputBuffer input;
    AnimationPlayer anim;
    BoxPresetApplier boxApplier;
    CollisionResolver resolver;

    // 필요하다면 PhysicsEntity나 기타도 참조
    PhysicsEntity phys;

    private TickMaster tickMaster;

    void Awake()
    {
        fsm = GetComponent<CharacterFSM>();
        input = GetComponent<InputBuffer>();
        anim = GetComponent<AnimationPlayer>();
        boxApplier = GetComponent<BoxPresetApplier>();
        resolver = GetComponent<CollisionResolver>();
        phys = GetComponent<PhysicsEntity>();
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

        // 3) 스킬/박스 스케줄 처리 (현재 프레임의 생성/소멸)
        boxApplier?.Tick();

        // 4) 애니메이션 프레임 전진(Playables 기반)
        anim?.Tick();

        // 5) 충돌 처리(본인 관점 우선순위 필터링 등)
        resolver?.Tick();

        // 6) (선택) 물리 후처리/디버그 훅
        // phys?.PostTick();
    }
}
