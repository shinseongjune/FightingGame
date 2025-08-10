using UnityEngine;

public class DebugFreeze : MonoBehaviour
{
    public bool frozen;          // UI로 토글
    public bool freezePhysics = true;

    Vector2 savedVel;
    bool physicsWasFrozen;

    public bool IsFrozen() => frozen;

    public void OnFreeze(PhysicsEntity pe)
    {
        if (frozen && freezePhysics && !physicsWasFrozen)
        {
            savedVel = pe.Velocity;
            pe.EnterKinematic();        // 중력/이동 정지
            physicsWasFrozen = true;
        }
        else if (!frozen && physicsWasFrozen)
        {
            pe.ReleaseFromCarry(true, savedVel);
            physicsWasFrozen = false;
        }
    }
}