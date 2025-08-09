using System.Collections.Generic;
using UnityEngine;
public enum PhysicsMode
{
    Normal,     // 중력+이동 처리 O
    Kinematic,  // 중력/가속 무시, 외부가 위치를 정함(잡힌 상태 등)
    Carried     // 특정 타겟을 따라감(공격자 손 위치 등)
}

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

    // 물리 처리 모드
    public PhysicsMode mode = PhysicsMode.Normal;

    // 충돌/히트 수신 토글
    public bool collisionsEnabled = true; // (BoxManager에서 owner 단위 필터)
    public bool receiveHits = true;       // (Hurt만 꺼야 할 때)
    public bool pushboxEnabled = true;    // (몸통 밀치기/겹침해소에 쓸 경우)

    // Carried 모드용
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

    // 편의 API
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
        collisionsEnabled = false; // 필요 시 피격/밀치기 모두 비활성
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