using UnityEngine;

[RequireComponent(typeof(CharacterFSM), typeof(CollisionResolver))]
public class CharacterCombatEvents : MonoBehaviour
{
    CharacterFSM fsm;
    CollisionResolver resolver;
    PhysicsEntity me;

    void Awake()
    {
        fsm = GetComponent<CharacterFSM>();
        resolver = GetComponent<CollisionResolver>();
        me = GetComponent<PhysicsEntity>();
    }

    void OnEnable()
    {
        resolver.OnHitResolved += HandleHit;
        resolver.OnGuardResolved += HandleGuard;
        resolver.OnThrowResolved += HandleThrow;
    }
    void OnDisable()
    {
        resolver.OnHitResolved -= HandleHit;
        resolver.OnGuardResolved -= HandleGuard;
        resolver.OnThrowResolved -= HandleThrow;
    }

    void HandleHit(HitData hit)
    {
        if (hit.taker == me) fsm.TransitionTo("Hit");      // 공중 여부에 따라 Hit_Air 등 분기
    }

    void HandleGuard(PhysicsEntity atk, PhysicsEntity def, CollisionData cd)
    {
        if (def == me) fsm.TransitionTo("Guarding");
    }

    void HandleThrow(PhysicsEntity atk, PhysicsEntity def, CollisionData cd)
    {
        if (def == me) fsm.TransitionTo("BeingThrown");
    }
}