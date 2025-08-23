using UnityEngine;

public static class CharacterWarp
{
    // 캐릭터를 즉시 특정 좌표로 이동(스폰/순간이동/잡기 연출 등)
    public static void Teleport(CharacterProperty prop, Vector2 pos, bool facingRight, bool resetVelocity, bool refreshBoxes)
    {
        if (!prop) return;

        var phys = prop.GetComponent<PhysicsEntity>();
        var box = prop.GetComponent<BoxPresetApplier>();

        prop.isFacingRight = facingRight;

        if (phys != null)
        {
            phys.Position = pos;
            if (resetVelocity) phys.Velocity = Vector2.zero;
            phys.SyncTransform(); // 트랜스폼 즉시 반영
        }

        if (refreshBoxes)
        {
            // 1) 동적 박스(히트/트리거)는 BoxManager가 틱마다 AABB를 다시 계산하므로 그대로 둬도 일반적으로 OK
            // 2) 그래도 같은 프레임 즉시 반영이 필요하면 박스 강제 갱신(옵션)
            var bm = BoxManager.Instance;
            bm?.NotifyOwnerMoved(prop.gameObject); // 아래 helper를 BoxManager에 추가(없어도 무해)
        }
    }
}
