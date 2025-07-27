public enum CollisionEventType
{
    Hit,
    Throw,
    Guard
}

public struct CollisionEvent
{
    public CollisionEventType type;
    public PhysicsEntity attacker;
    public BoxComponent attackerBox;

    public PhysicsEntity target;
    public BoxComponent targetBox;
}
