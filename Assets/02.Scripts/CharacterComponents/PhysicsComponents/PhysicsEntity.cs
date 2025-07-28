using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 캐릭터나 오브젝트에 부착되는 물리 엔티티.
/// 자신의 박스를 기반으로 움직이며, Resolver에 의해 충돌/해소됨.
/// </summary>
[RequireComponent(typeof(CharacterProperty))]
public class PhysicsEntity : MonoBehaviour, ITicker
{
    [Tooltip("디버그 전용")]
    public bool autoRefreshBoxes;

    public Vector2 velocity;
    public float gravity = -20f;
    public bool grounded;

    private List<BoxComponent> boxes = new();
    public IReadOnlyList<BoxComponent> Boxes => boxes;

    public BoxComponent BodyBox { get; set; }

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
    /// Tick마다 호출: 중력 및 속도 적용 전 단계
    /// </summary>
    public void Tick()
    {
        if (autoRefreshBoxes) // Debug 또는 테스트용
            RefreshBoxes();

        ApplyGravity();
    }

    private void ApplyGravity()
    {
        if (!grounded)
            velocity.y += gravity * TickMaster.TICK_INTERVAL;
    }

    /// <summary>
    /// 이동 벡터 적용 (해소 등)
    /// </summary>
    public void ApplyOffset(Vector2 offset)
    {
        transform.position += (Vector3)offset;
    }

    /// <summary>
    /// 이동 속도 반영 (매 틱마다 호출)
    /// </summary>
    public void ApplyVelocity()
    {
        transform.position += (Vector3)(velocity * TickMaster.TICK_INTERVAL);
    }

    /// <summary>
    /// 현재 보유 중인 BoxComponent를 다시 수집
    /// 외부에서 박스 정보를 갱신해야 할 경우 호출
    /// </summary>
    public void RefreshBoxes()
    {
        boxes.Clear();
        GetComponentsInChildren<BoxComponent>(includeInactive: true, result: boxes);
        BodyBox = boxes.Find(b => b.type == BoxType.Body);
    }
}
