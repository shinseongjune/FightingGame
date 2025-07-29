public interface IHitReceiver
{
    void OnHit(PhysicsEntity attacker, BoxComponent hitBox, BoxComponent hurtBox);
}