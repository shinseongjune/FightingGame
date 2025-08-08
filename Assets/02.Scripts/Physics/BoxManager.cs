using System;
using System.Collections.Generic;
using UnityEngine;

public class BoxManager : Singleton<BoxManager>, ITicker
{
    public List<BoxComponent> activeBoxes = new();

    public event Action<CollisionData> OnCollision;

    public void Register(BoxComponent box) => activeBoxes.Add(box);
    public void Unregister(BoxComponent box) => activeBoxes.Remove(box);

    public void Tick()
    {
        for (int i = 0; i < activeBoxes.Count; i++)
        {
            if (!activeBoxes[i].gameObject.activeSelf) continue;
            
            for (int j = i + 1; j < activeBoxes.Count; j++)
            {
                var a = activeBoxes[i];
                var b = activeBoxes[j];
                
                if (!b.gameObject.activeSelf) continue;
                
                if (AABBCheck(a, b))
                {
                    OnCollision?.Invoke(new CollisionData{ boxA = a, boxB = b, hitPoint = a.GetHitPoint(b) });
                }
            }
        }
    }

    bool AABBCheck(BoxComponent a, BoxComponent b)
        => a.GetAABB().Overlaps(b.GetAABB());
}
