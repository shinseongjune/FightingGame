using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 캐릭터나 오브젝트에 부착되는 물리 엔티티.
/// 자신의 박스를 기반으로 움직이며, Resolver에 의해 충돌/해소됨.
/// </summary>
[RequireComponent(typeof(CharacterProperty))]
public class PhysicsEntity : MonoBehaviour, ITicker, IHitReceiver, IThrowReceiver, IGuardReceiver
{
    [Tooltip("디버그 전용")]
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

    public void OnHit(PhysicsEntity attacker, BoxComponent hitBox, BoxComponent hurtBox)
    {
        Debug.Log($"[Hit] {name} was hit by {attacker.name}");

        var prop = GetComponent<CharacterProperty>();
        Vector2 hitPoint = EstimateHitPoint(hitBox, hurtBox);
        Vector2 toAttacker = attacker.transform.position - transform.position;
        bool isFacingRight = GetComponent<CharacterProperty>().isFacingRight;

        var attackerProperty = attacker.GetComponent<CharacterProperty>();
        var currentSkill = attackerProperty != null ? attackerProperty.currentSkill : null;

        prop.lastHitInfo = new LastHitInfo
        {
            attacker = attacker,
            hitBox = hitBox,
            hurtBox = hurtBox,
            hitPoint = hitPoint,

            direction = hitBox.direction,
            fromFront = Vector2.Dot(toAttacker.normalized, isFacingRight ? Vector2.right : Vector2.left) >= 0,
            region = DetermineRegion(hitPoint),

            damage = currentSkill != null ? currentSkill.damageOnHit : 0,
            hitStun = currentSkill != null ? currentSkill.hitstunDuration : 0,
            blockStun = currentSkill != null ? currentSkill.blockstunDuration : 0,
            launches = currentSkill != null ? currentSkill.causesLaunch : false,
            causesKnockdown = currentSkill != null ? currentSkill.causesKnockdown : false
        };
    }

    public void OnThrow(PhysicsEntity thrower, BoxComponent throwBox, BoxComponent bodyBox)
    {
        Debug.Log($"[Throw] {name} was thrown by {thrower.name}");

        var fsm = bodyBox.GetComponentInParent<CharacterFSM>();
        if (fsm != null)
        {
            fsm.TransitionTo(new BeingThrownState(fsm));
        }
        else
        {
            Debug.LogWarning("[Throw] 대상에 FSM이 없습니다.");
        }
    }

    public void OnGuardTrigger(PhysicsEntity threat, BoxComponent triggerBox, BoxComponent bodyBox)
    {
        Debug.Log($"[GuardTrigger] {name} is forced to guard due to {threat.name}");

        // TODO: 상태 전환, 가드 입력 비교, 애니메이션 전환 등
        // 예: if (isHoldingBack) → 성공적인 가드
    }

    Vector2 EstimateHitPoint(BoxComponent hit, BoxComponent hurt)
    {
        // 간단히 중앙값 평균
        return (hit.WorldBounds.center + hurt.WorldBounds.center) * 0.5f;
    }

    bool DetermineFront(Vector2 attackerPos, Vector2 targetPos, bool targetFacingRight)
    {
        Vector2 toAttacker = attackerPos - targetPos;
        return Vector2.Dot(toAttacker.normalized, targetFacingRight ? Vector2.right : Vector2.left) >= 0;
    }

    HitRegion DetermineRegion(Vector2 hitPoint)
    {
        return HitRegion.Body;
        //TODO: 임시처리. 머리/몸통/다리 위치 가져와서 비교할 것.
    }
}
