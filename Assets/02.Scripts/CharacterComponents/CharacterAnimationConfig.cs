using UnityEngine;

[RequireComponent(typeof(AnimationPlayer))]
public class CharacterAnimationConfig : MonoBehaviour
{
    public CharacterAnimSet_SO animSet;

    public string GetClipKey(AnimKey key)
    {
        if (animSet != null && animSet.TryGet(key, out var name)) return name;
        Debug.LogWarning($"[AnimConfig] No mapping for {key}");
        return null;
    }
}
