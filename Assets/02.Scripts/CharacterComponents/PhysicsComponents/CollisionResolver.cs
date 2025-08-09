using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PhysicsEntity))]
public class CollisionResolver : MonoBehaviour, ITicker
{
    private readonly List<CollisionData> frameCollisions = new();
    private PhysicsEntity me;

    // 외부로 내보낼 이벤트 (원하면 FSM에서 구독)
    public event Action<HitData> OnHitResolved;
    public event Action<PhysicsEntity, PhysicsEntity, CollisionData> OnThrowResolved;
    public event Action<PhysicsEntity, PhysicsEntity, CollisionData> OnGuardResolved;

    private void Awake()
    {
        me = GetComponent<PhysicsEntity>();
    }

    void OnEnable() { BoxManager.Instance.OnCollision += OnCollision; }
    void OnDisable() { BoxManager.Instance.OnCollision -= OnCollision; }

    void OnCollision(CollisionData data)
    {
        // 내 박스가 연루된 충돌만 모은다
        if (data?.boxA?.owner == me || data?.boxB?.owner == me)
            frameCollisions.Add(data);
    }

    public void Tick()
    {
        if (frameCollisions.Count == 0) return;

        // 한 틱에 내 캐릭터에 대해 정확히 1건만 처리
        CollisionData winner = null;
        int winnerPrio = int.MinValue;

        for (int i = 0; i < frameCollisions.Count; i++)
        {
            var cd = frameCollisions[i];
            if (cd?.boxA == null || cd.boxB == null) continue;

            // 내 박스 / 상대 박스
            bool iAmA = cd.boxA.owner == me;
            var myBox = iAmA ? cd.boxA : cd.boxB;
            var otherBox = iAmA ? cd.boxB : cd.boxA;

            // 내가 관련 없는 이벤트면 스킵(방어)
            if (myBox?.owner != me) continue;

            // 타입 판정
            // 유효: (Hit|Throw|GuardTrigger) ↔ Hurt
            int prio = Priority(myBox, otherBox);
            if (prio <= 0) continue;

            // 타이브레이커(동일 우선순위 시): 더 큰 겹침 면적 → 더 먼저 발견된 것
            if (prio > winnerPrio || (prio == winnerPrio && OverlapArea(cd) > OverlapArea(winner)))
            {
                winnerPrio = prio;
                winner = cd;
            }
        }

        if (winner != null)
        {
            // 승자 1건만 확정 처리
            ApplyWinner(winner);
        }

        frameCollisions.Clear();
    }

    private void ResolveHit(CollisionData cd)
    {
        var (atkBox, defBox) = AttackerDefender(cd);
        var hit = new HitData
        {
            collision = cd,
            attacker = atkBox.owner,
            taker = defBox.owner,
            skill = null, // 필요하면 시전자 컴포넌트(예: SkillExecutor)에서 채워 넣기
            height = ClassifyHitHeight(defBox, cd.hitPoint),
            direction = ClassifyHitDirection(atkBox.owner, defBox.owner, cd.hitPoint),
        };
        OnHitResolved?.Invoke(hit);
        // TODO: FSM 전이/HP감소/넉백 등 연결
    }

    private void ResolveThrow(CollisionData cd)
    {
        var (atkBox, defBox) = AttackerDefender(cd);
        OnThrowResolved?.Invoke(atkBox.owner, defBox.owner, cd);
        // TODO: FSM 전이(BeingThrown 등) 연결
    }

    private void ResolveGuard(CollisionData cd)
    {
        var (atkBox, defBox) = AttackerDefender(cd);
        OnGuardResolved?.Invoke(atkBox.owner, defBox.owner, cd);
        // TODO: 가드 상태 전이/가드 경직 적용 등
    }

    // --- 유틸 ---

    // Hit > Throw > GuardTrigger
    private static int Priority(BoxComponent my, BoxComponent other)
    {
        // 내 입장에서: 내가 Hurt면 상대의 Hit/Throw/GuardTrigger가 ‘나에게 적용될’ 이벤트.
        // 내가 Hit/Throw/GuardTrigger면 ‘내가 가한’ 이벤트인데, 같은 틱에 "내가 피격" 이벤트와 경합하면
        // 전역(BoxManager)에서 이미 페어 우선순위로 1건만 왔을 것이고,
        // 혹시 둘 다 들어오더라도 여기서 Hit > Throw > Guard로 정리됨.
        if (IsPair(my, other, BoxType.Hurt, BoxType.Hit)) return 3;
        if (IsPair(my, other, BoxType.Hurt, BoxType.Throw)) return 2;
        if (IsPair(my, other, BoxType.Hurt, BoxType.GuardTrigger)) return 1;

        // 공격 측일 때도 우선순위를 동일하게 두어 한 틱 하나만 선택되게 함
        if (IsPair(my, other, BoxType.Hit, BoxType.Hurt)) return 3;
        if (IsPair(my, other, BoxType.Throw, BoxType.Hurt)) return 2;
        if (IsPair(my, other, BoxType.GuardTrigger, BoxType.Hurt)) return 1;

        return 0;
    }

    private static bool IsPair(BoxComponent a, BoxComponent b, BoxType x, BoxType y)
        => (a.type == x && b.type == y) || (a.type == y && b.type == x);

    private static float OverlapArea(CollisionData cd)
    {
        if (cd == null) return -1f;
        Rect r1 = cd.boxA.GetAABB();
        Rect r2 = cd.boxB.GetAABB();
        float w = Mathf.Max(0, Mathf.Min(r1.xMax, r2.xMax) - Mathf.Max(r1.xMin, r2.xMin));
        float h = Mathf.Max(0, Mathf.Min(r1.yMax, r2.yMax) - Mathf.Max(r1.yMin, r2.yMin));
        return w * h;
    }

    private void ApplyWinner(CollisionData cd)
    {
        var (atkBox, defBox) = AttackerDefender(cd);

        if (IsPair(atkBox, defBox, BoxType.Hit, BoxType.Hurt))
        {
            ResolveHit(cd);
        }
        else if (IsPair(atkBox, defBox, BoxType.Throw, BoxType.Hurt))
        {
            ResolveThrow(cd);
        }
        else if (IsPair(atkBox, defBox, BoxType.GuardTrigger, BoxType.Hurt))
        {
            ResolveGuard(cd);
        }
    }

    private static (BoxComponent attacker, BoxComponent defender) AttackerDefender(CollisionData cd)
    {
        var a = cd.boxA; var b = cd.boxB;
        if (a.type == BoxType.Hurt && b.type != BoxType.Hurt) return (b, a);
        if (b.type == BoxType.Hurt && a.type != BoxType.Hurt) return (a, b);
        return (a, b); // 안전망
    }

    private static HitHeight ClassifyHitHeight(BoxComponent defenderHurt, Vector2 hitPoint)
    {
        Rect hr = defenderHurt.GetAABB();
        float h = hr.height;
        float lowTop = hr.yMin + h / 3f;
        float midTop = hr.yMin + 2f * h / 3f;

        if (hitPoint.y < lowTop) return HitHeight.Low;
        if (hitPoint.y < midTop) return HitHeight.Middle;
        return HitHeight.High;
    }

    private static HitDirection ClassifyHitDirection(PhysicsEntity attacker, PhysicsEntity defender, Vector2 hitPoint)
    {
        // 가장 단순한 버전: 상대 중심 대비 좌/우 + 위/아래
        Vector2 d = hitPoint - defender.Position;
        if (Mathf.Abs(d.x) >= Mathf.Abs(d.y))
            return d.x >= 0 ? HitDirection.Right : HitDirection.Left;
        else
            return d.y >= 0 ? HitDirection.Up : HitDirection.Down;
    }
}
