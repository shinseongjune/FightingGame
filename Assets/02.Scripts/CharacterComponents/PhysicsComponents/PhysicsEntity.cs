using System.Collections.Generic;
using UnityEngine;

public class PhysicsEntity : MonoBehaviour
{
    public Vector2 Position;
    public Vector2 Velocity;
    public bool isGravityOn = true;
    public bool isGrounded = false;
    public float groundY = 0f;

    // 현재 활성 bodybox
    public BoxComponent currentBodyBox;
    // 현재 활성 wiffbox
    public List<BoxComponent> currentWhiffBoxes;

    // 자세별 바디박스 프리셋
    public BoxComponent idleBodyBox;
    public List<BoxComponent> idleWhiffBoxes;

    public BoxComponent crouchBodyBox;
    public List<BoxComponent> crouchWhiffBoxes;

    public BoxComponent jumpBodyBox;
    public List<BoxComponent> jumpWhiffBoxes;

    public BoxComponent downBodyBox;

    public void SetPose(CharacterStateTag state)
    {
        switch (state)
        {
            case CharacterStateTag.Idle:
                currentBodyBox = idleBodyBox;
                currentWhiffBoxes = idleWhiffBoxes;
                break;
            case CharacterStateTag.Crouch:
                currentBodyBox = crouchBodyBox;
                currentWhiffBoxes = crouchWhiffBoxes;
                break;
            case CharacterStateTag.Jump_Up:
            case CharacterStateTag.Jump_Forward:
            case CharacterStateTag.Jump_Backward:
                currentBodyBox = jumpBodyBox;
                currentWhiffBoxes = jumpWhiffBoxes;
                break;
            case CharacterStateTag.Knockdown:
            case CharacterStateTag.HardKnockdown:
                currentBodyBox = downBodyBox;
                currentWhiffBoxes = null;
                break;
        }
    }
}