public interface IGuardReceiver
{
    void OnGuardTrigger(PhysicsEntity threat, BoxComponent triggerBox, BoxComponent bodyBox);
}