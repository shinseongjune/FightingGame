using System;
using UnityEngine;

public class StageSetup : MonoBehaviour
{
    [Serializable]
    public class StageBounds
    {
        [Header("World X clamp (left < right)")]
        public float leftX = -8f;
        public float rightX = 8f;

        [Header("Ground Y (착지선)")]
        public float groundY = 0f;

        [Header("Optional: 천장")]
        public bool hasCeiling = false;
        public float ceilingY = 6f;
    }

    public StageBounds bounds;
    public float wallHeight = 6f;
    public float wallThickness = 0.5f; // Body 박스 폭

    void Start()
    {
        MakeWall("LeftWall", bounds.leftX, bounds.groundY, wallHeight);
        MakeWall("RightWall", bounds.rightX, bounds.groundY, wallHeight);
    }

    void MakeWall(string name, float x, float groundY, float height)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(x, groundY, 0);

        var pe = go.AddComponent<PhysicsEntity>();
        pe.mode = PhysicsMode.Kinematic;
        pe.pushboxEnabled = true;
        pe.immovablePushbox = true;
        pe.isGravityOn = false;

        // Body 박스 1개 세팅
        pe.idleBody = new PhysicsEntity.BoxNum
        {
            center = new Vector2(0f, height * 0.5f),
            size = new Vector2(wallThickness, height)
        };
        pe.BuildDefaultBoxesFromNumbers();
        pe.SetPose(CharacterStateTag.Idle);
    }
}