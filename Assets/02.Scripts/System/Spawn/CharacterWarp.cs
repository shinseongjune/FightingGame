using UnityEngine;

public static class CharacterWarp
{
    // ĳ���͸� ��� Ư�� ��ǥ�� �̵�(����/�����̵�/��� ���� ��)
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
            phys.SyncTransform(); // Ʈ������ ��� �ݿ�
        }

        if (refreshBoxes)
        {
            // 1) ���� �ڽ�(��Ʈ/Ʈ����)�� BoxManager�� ƽ���� AABB�� �ٽ� ����ϹǷ� �״�� �ֵ� �Ϲ������� OK
            // 2) �׷��� ���� ������ ��� �ݿ��� �ʿ��ϸ� �ڽ� ���� ����(�ɼ�)
            var bm = BoxManager.Instance;
            bm?.NotifyOwnerMoved(prop.gameObject); // �Ʒ� helper�� BoxManager�� �߰�(��� ����)
        }
    }
}
