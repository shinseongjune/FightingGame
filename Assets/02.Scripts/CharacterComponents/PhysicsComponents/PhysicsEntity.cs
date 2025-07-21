using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ĳ���ͳ� ������Ʈ�� �����Ǵ� ���� ��ƼƼ.
/// �ڽ��� �ڽ��� ������� �����̸�, Resolver�� ���� �浹/�ؼҵ�.
/// </summary>
[RequireComponent(typeof(CharacterProperty))]
public class PhysicsEntity : MonoBehaviour, ITicker, IHitReceiver, IThrowReceiver, IGuardReceiver
{
    [Tooltip("����� ����")]
    public bool autoRefreshBoxes;

    public Vector2 velocity;
    public float gravity = -20f;
    public bool grounded;

    private List<BoxComponent> boxes = new();
    public IReadOnlyList<BoxComponent> Boxes => boxes;

    public BoxComponent BodyBox { get; private set; }

    private void Awake()
    {
        RefreshBoxes();
        PhysicsResolver.Instance.Register(this);
    }

    private void OnDestroy()
    {
        if (PhysicsResolver.Instance != null)
            PhysicsResolver.Instance.Unregister(this);
    }

    /// <summary>
    /// Tick���� ȣ��: �߷� �� �ӵ� ���� �� �ܰ�
    /// </summary>
    public void Tick()
    {
        if (autoRefreshBoxes) // Debug �Ǵ� �׽�Ʈ��
            RefreshBoxes();

        ApplyGravity();
    }

    private void ApplyGravity()
    {
        if (!grounded)
            velocity.y += gravity * TickMaster.TICK_INTERVAL;
    }

    /// <summary>
    /// �̵� ���� ���� (�ؼ� ��)
    /// </summary>
    public void ApplyOffset(Vector2 offset)
    {
        transform.position += (Vector3)offset;
    }

    /// <summary>
    /// �̵� �ӵ� �ݿ� (�� ƽ���� ȣ��)
    /// </summary>
    public void ApplyVelocity()
    {
        transform.position += (Vector3)(velocity * TickMaster.TICK_INTERVAL);
    }

    /// <summary>
    /// ���� ���� ���� BoxComponent�� �ٽ� ����
    /// �ܺο��� �ڽ� ������ �����ؾ� �� ��� ȣ��
    /// </summary>
    public void RefreshBoxes()
    {
        boxes.Clear();
        GetComponentsInChildren<BoxComponent>(includeInactive: true, result: boxes);
        BodyBox = boxes.Find(b => b.type == BoxType.Body);
    }

    public void OnHit(PhysicsEntity attacker, BoxComponent hitBox, BoxComponent hurtBox)
    {
        Debug.Log($"[Hit] {name} was hit by {attacker.name}");

        // ������ ���, ���� ����, �˹� ��
        // ���� SkillExecutor ��� ���� ����
    }

    public void OnThrow(PhysicsEntity thrower, BoxComponent throwBox, BoxComponent bodyBox)
    {
        Debug.Log($"[Throw] {name} was thrown by {thrower.name}");

        // TODO: ��� ����, ���� ����, ��ġ �̵� ��
    }

    public void OnGuardTrigger(PhysicsEntity threat, BoxComponent triggerBox, BoxComponent bodyBox)
    {
        Debug.Log($"[GuardTrigger] {name} is forced to guard due to {threat.name}");

        // TODO: ���� ��ȯ, ���� �Է� ��, �ִϸ��̼� ��ȯ ��
        // ��: if (isHoldingBack) �� �������� ����
    }
}
