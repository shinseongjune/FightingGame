using UnityEngine;

public class BoxDebugDrawer : MonoBehaviour
{
    public bool visible = true;
    public Color body = new Color(0f, 0.8f, 1f, 0.25f);
    public Color hit = new Color(1f, 0f, 0f, 0.25f);
    public Color hurt = new Color(0f, 1f, 0f, 0.25f);
    public Color thrw = new Color(1f, 0.5f, 0f, 0.25f);
    public Color guard = new Color(1f, 1f, 0f, 0.25f);

    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.F1)) visible = !visible;
    }

    void OnDrawGizmos()
    {
        if (!visible) return;

        var mgr = BoxManager.Instance;
        if (mgr == null || mgr.activeBoxes == null) return;

        // Ȥ�� �ٸ� ��ũ��Ʈ�� Gizmos.matrix�� �ٲ��� �� ������ �ʱ�ȭ
        Gizmos.matrix = Matrix4x4.identity;

        // ���� �β��� �༭ 2D������ Ȯ���� ���̰�
        const float zThickness = 0.02f;

        // foreach �߰� ���� ���� ȸ��: ������ ���� ��ȸ
        var list = mgr.activeBoxes;
        for (int i = 0; i < list.Count; i++)
        {
            var b = list[i];
            if (b == null || b.owner == null) continue;

            Rect r = b.GetAABB();
            var center = new Vector3(r.center.x, r.center.y, 0f);
            var size3 = new Vector3(r.size.x, r.size.y, zThickness);

            Gizmos.color = b.type switch
            {
                BoxType.Body => body,
                BoxType.Hit => hit,
                BoxType.Hurt => hurt,
                BoxType.Throw => thrw,
                BoxType.GuardTrigger => guard,
                _ => Color.white
            };

            Gizmos.DrawCube(center, size3);

            // �ܰ����� �������ϰ�
            var edge = Gizmos.color; edge.a = 1f;
            Gizmos.color = edge;
            Gizmos.DrawWireCube(center, size3);
        }
    }
}