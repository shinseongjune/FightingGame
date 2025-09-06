using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterFSM))]
[RequireComponent(typeof(PhysicsEntity))]
[RequireComponent(typeof(CharacterProperty))]
public class DebugAutoDriver : MonoBehaviour, ITicker
{
    [Header("Wiring")]
    public BattleManager gm;                 // ����θ� �ڵ� ã��
    public Skill_SO skillToUse;            // �ֱ������� �� ��ų(�ɼ�)

    [Header("Move Pattern")]
    public float moveSpeed = 2.0f;         // �¿� �̵� �ӵ�(����/��)
    public int moveFrames = 30;            // �̵� ���� ������ ��
    public int waitFrames = 20;            // ���� ��� ������ ��

    [Header("Attack Pattern")]
    public int attackEveryNFrames = 60;    // N�����Ӹ��� ��ų ���(0�̸� ��� ����)

    CharacterFSM fsm;
    PhysicsEntity phys;
    CharacterProperty prop;

    int frame;    // ��ü ������ ����
    int phase;    // 0,1,0,1�� ¦��=��, Ȧ��=��
    int local;    // �� �ܰ迡�� ��� ������

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
        // ���巹��: ���� �帧/�Է� ���/����� �÷���
        if (gm == null || !gm.enabled || !gameObject.activeInHierarchy) return;
        if (!gm.debugAuto) return;
        if (prop != null && !prop.isInputEnabled) return;

        frame++;
        local++;

        // ----- �¿� �պ� ���� -----
        int dir = (phase % 2 == 0) ? +1 : -1; // ¦��=������, Ȧ��=����
        if (local <= moveFrames)
        {
            float dx = dir * moveSpeed * TickMaster.TICK_INTERVAL;
            phys.Position += new Vector2(dx, 0f);
            phys.SyncTransform(); // ��� �ݿ�(����/�ڽ� ��ġ)
        }
        else if (local > moveFrames + waitFrames)
        {
            local = 0;
            phase++;
            // �ٶ󺸴� ���� ����(����): �̵� ���� ����
            //if (prop != null) prop.isFacingRight = (dir > 0);
        }

        // ----- �ֱ��� ���� -----
        if (attackEveryNFrames > 0 && skillToUse != null && (frame % attackEveryNFrames) == 0)
        {
            prop.currentSkill = skillToUse;
            fsm.TransitionTo("Skill");
        }
    }
}
