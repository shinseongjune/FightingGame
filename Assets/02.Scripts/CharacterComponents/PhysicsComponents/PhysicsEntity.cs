using System.Collections.Generic;
using UnityEngine;
public enum PhysicsMode
{
    Normal,     // �߷�+�̵� ó�� O
    Kinematic,  // �߷�/���� ����, �ܺΰ� ��ġ�� ����(���� ���� ��)
    Carried     // Ư�� Ÿ���� ����(������ �� ��ġ ��)
}

public class PhysicsEntity : MonoBehaviour
{
    public Vector2 Position;
    public Vector2 Velocity;
    public bool isGravityOn = true;
    public bool isGrounded = false;
    public float groundY = 0f;

    // ���� Ȱ�� bodybox
    public BoxComponent currentBodyBox;
    // ���� Ȱ�� wiffbox
    public List<BoxComponent> currentWhiffBoxes;

    // �ڼ��� �ٵ�ڽ� ������
    public BoxComponent idleBodyBox;
    public List<BoxComponent> idleWhiffBoxes;

    public BoxComponent crouchBodyBox;
    public List<BoxComponent> crouchWhiffBoxes;

    public BoxComponent jumpBodyBox;
    public List<BoxComponent> jumpWhiffBoxes;

    public BoxComponent downBodyBox;

    // ���� ó�� ���
    public PhysicsMode mode = PhysicsMode.Normal;

    // �浹/��Ʈ ���� ���
    public bool collisionsEnabled = true; // (BoxManager���� owner ���� ����)
    public bool receiveHits = true;       // (Hurt�� ���� �� ��)
    public bool pushboxEnabled = true;    // (���� ��ġ��/��ħ�ؼҿ� �� ���)

    // Carried ����
    public PhysicsEntity followTarget;
    public Vector2 followOffset;

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

    // ���� API
    public void EnterKinematic()
    {
        mode = PhysicsMode.Kinematic;
        isGravityOn = false;
        Velocity = Vector2.zero;
        isGrounded = false;
    }

    public void AttachTo(PhysicsEntity target, Vector2 offset)
    {
        mode = PhysicsMode.Carried;
        followTarget = target;
        followOffset = offset;
        isGravityOn = false;
        Velocity = Vector2.zero;
        isGrounded = false;
        collisionsEnabled = false; // �ʿ� �� �ǰ�/��ġ�� ��� ��Ȱ��
        receiveHits = false;
    }

    public void ReleaseFromCarry(bool reenableCollisions, Vector2 launchVelocity)
    {
        mode = PhysicsMode.Normal;
        followTarget = null;
        isGravityOn = true;
        Velocity = launchVelocity;
        collisionsEnabled = reenableCollisions;
        receiveHits = true;
    }
}