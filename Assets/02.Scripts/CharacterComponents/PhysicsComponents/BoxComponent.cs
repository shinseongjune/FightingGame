using UnityEngine;

public enum BoxType
{
    Map,
    Body,
    Hit,
    Hurt,
    Throw,
    GuardTrigger
}

[ExecuteAlways]
public class BoxComponent : MonoBehaviour
{
    [Header("Box Settings")]
    public BoxType type;
    public Vector2 center = Vector2.zero;
    public Vector2 size = Vector2.one;
    public int layer = 0;

    public bool IsTrigger => type != BoxType.Body && type != BoxType.Map;
    public bool IsEnabled => enabled && gameObject.activeInHierarchy;

    public HitDirection direction = HitDirection.Front;

    public Rect WorldBounds
    {
        get
        {
            Vector2 worldCenter = (Vector2)transform.position + center;
            return new Rect(worldCenter - size * 0.5f, size);
        }
    }

    public Vector2 TopLeft => (Vector2)transform.position + center + new Vector2(-size.x, size.y) * 0.5f;
    public Vector2 BottomRight => (Vector2)transform.position + center + new Vector2(size.x, -size.y) * 0.5f;

    private void OnDrawGizmos()
    {
        if (!IsEnabled) return;

        Gizmos.color = GetBoxColor(type);
        Rect r = WorldBounds;
        Gizmos.DrawWireCube(r.center, r.size);
    }

    private Color GetBoxColor(BoxType type)
    {
        return type switch
        {
            BoxType.Map => Color.gray7,
            BoxType.Body => Color.gray,
            BoxType.Hit => Color.yellow,
            BoxType.Hurt => Color.green,
            BoxType.Throw => new Color(1f, 0.5f, 0f),
            BoxType.GuardTrigger => Color.red,
            _ => Color.white,
        };
    }
}
