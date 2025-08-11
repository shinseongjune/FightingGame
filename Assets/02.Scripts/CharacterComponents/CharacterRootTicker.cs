using UnityEngine;

[RequireComponent(typeof(CharacterFSM))]
public class CharacterRootTicker : MonoBehaviour, ITicker
{
    CharacterFSM fsm;
    InputBuffer input;
    AnimationPlayer anim;
    BoxPresetApplier boxApplier;
    CollisionResolver resolver;

    // �ʿ��ϴٸ� PhysicsEntity�� ��Ÿ�� ����
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

    // �� ƽ�� ȣ�� ������ �����⼭�� ����
    public void Tick()
    {
        // 0) (����) ���� �������� ĳ���� ���ο� �ʿ��ϸ� ���⼭
        // phys?.PreTick(); // ���� ������

        // 1) �Է� ����
        input?.Tick();

        // 2) FSM(����.OnTick) : Ŀ�ǵ� �ν�/���� ����/�÷��� ������Ʈ
        fsm?.Tick();

        // 3) ��ų/�ڽ� ������ ó�� (���� �������� ����/�Ҹ�)
        boxApplier?.Tick();

        // 4) �ִϸ��̼� ������ ����(Playables ���)
        anim?.Tick();

        // 5) �浹 ó��(���� ���� �켱���� ���͸� ��)
        resolver?.Tick();

        // 6) (����) ���� ��ó��/����� ��
        // phys?.PostTick();
    }
}
