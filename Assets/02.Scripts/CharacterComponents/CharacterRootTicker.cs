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

    // �� ƽ�� ȣ�� ������ �����⼭�� ����
    public void Tick()
    {
        // 0) (����) ���� �������� ĳ���� ���ο� �ʿ��ϸ� ���⼭
        // phys?.PreTick(); // ���� ������

        // 1) �Է� ����
        input?.Tick();

        // 2) FSM(����.OnTick) : Ŀ�ǵ� �ν�/���� ����/�÷��� ������Ʈ
        fsm?.Tick();

        // 3) �ִϸ��̼� ������ ����(Playables ���)
        anim?.Tick();

        // 4) ��ų/�ڽ� ������ ó�� (���� �������� ����/�Ҹ�)
        boxApplier?.Tick();

        // 5) �浹 ó��(���� ���� �켱���� ���͸� ��)
        resolver?.Tick();

        TryAutoFaceWhenBothGrounded();
    }

    void TryAutoFaceWhenBothGrounded()
    {
        var me = GetComponent<CharacterProperty>();
        if (me == null) return;
        var myPhys = GetComponent<PhysicsEntity>();
        if (myPhys == null) return;

        // ��� ã��(2�� ����)
        CharacterProperty enemy = null;
        foreach (var c in GameObject.FindObjectsByType<CharacterProperty>(FindObjectsSortMode.None))
            if (c != me) { enemy = c; break; }
        if (enemy == null) return;

        var enPhys = enemy.GetComponent<PhysicsEntity>();
        if (enPhys == null) return;

        // 1) ���� ��� ������ ����
        if (!(myPhys.isGrounded && enPhys.isGrounded)) return;

        // 3) �����׸��ý�(����/���� X���� �¿� Ƣ�� �� ����)
        const float swapThreshold = 0.05f; // �������� ���� ����, �ʿ�� 0.1~0.2
        float dx = enPhys.Position.x - myPhys.Position.x;
        if (Mathf.Abs(dx) < swapThreshold) return;

        bool meShouldFaceRight = dx > 0f;

        me.SetFacing(meShouldFaceRight);
    }
}
