public interface IThrowReceiver
{
    void OnThrow(PhysicsEntity thrower, BoxComponent throwBox, BoxComponent bodyBox);
}