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
        if (Input.GetKeyDown(KeyCode.F1)) visible = !visible;
    }

    void OnDrawGizmos()
    {
        if (!visible || BoxManager.Instance == null) return;
        foreach (var b in BoxManager.Instance.activeBoxes)
        {
            if (b == null || b.owner == null) continue;
            Rect r = b.GetAABB();
            Gizmos.color = b.type switch
            {
                BoxType.Body => body,
                BoxType.Hit => hit,
                BoxType.Hurt => hurt,
                BoxType.Throw => thrw,
                BoxType.GuardTrigger => guard,
                _ => Color.white
            };
            Gizmos.DrawCube(r.center, r.size);
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(r.center, r.size);
        }
    }
}