using UnityEngine;

public enum BoxType { Body, Hit, Hurt, Throw, GuardTrigger }

public class BoxComponent : MonoBehaviour
{
    public BoxType type;
    public Vector2 offset;
    public Vector2 size;
    public PhysicsEntity owner;
    public Skill_SO sourceSkill;
    public int uid;

    public Rect GetAABB()
    {
        var pos = owner.Position;
        int sign = 1;
        if (owner.property)
        {
            sign = owner.property.isFacingRight ? 1 : -1;
        }
        Vector2 center = new Vector2(pos.x + sign * offset.x, pos.y + offset.y);
        Vector2 half = size * 0.5f;
        return new Rect(center - half, size);
    }

    public Vector2 GetHitPoint(BoxComponent other)
    {
        Vector2 thisPos = owner.Position + offset;
        Vector2 otherPos = other.owner.Position + other.offset;
        return (thisPos + otherPos) / 2;
    }
}

public class CollisionData
{
    public BoxComponent boxA;
    public BoxComponent boxB;
    public Vector2 hitPoint;
}

public class HitData
{
    public CollisionData collision;
    public PhysicsEntity attacker;
    public PhysicsEntity taker;
    public Skill_SO skill;
    public HitHeight height;
    public HitDirection direction;
}

/// <summary>
/// 공격의 방향
/// </summary>
public enum HitDirection
{
    Front,
    Left,
    Right,
    Up,
    Down
}

/// <summary>
/// 맞은 부위 높이
/// </summary>
public enum HitHeight
{
    High,
    Middle,
    Low,
}
