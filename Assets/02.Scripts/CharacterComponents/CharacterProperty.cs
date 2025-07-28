using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 공격의 방향
/// </summary>
public enum HitDirection
{
    Front,
    Left,
    Right,
    Up,
    Down
}

/// <summary>
/// 맞은 부위 높이
/// </summary>
public enum HitRegion
{
    Head,
    Body,
    Legs
}

public struct LastHitInfo
{
    public PhysicsEntity attacker;
    public BoxComponent hitBox;
    public BoxComponent hurtBox;

    public Vector2 hitPoint;      // 적중 위치 (AABB 기반 추정)
    public HitDirection direction; // 애니메이션 연출용 방향
    public bool fromFront;        // 실제로 정면에서 맞았는지 (false면서 direction이 front인 경우 뒤에서 맞은 것으로 취급)
    public HitRegion region;      // 신체 부위 분류

    public int damage;
    public int hitStun;
    public int blockStun;
    public bool launches;
    public bool causesKnockdown;
}

public enum CharacterStateTag
{
    Standing,
    Crouching,
    Jumping,
    Down,
}

public class CharacterProperty : MonoBehaviour
{// 항상 존재하는 바디 박스 (움직임/충돌용)
    public BoxComponent idleBodyBox;
    public BoxComponent crouchBodyBox;
    public BoxComponent jumpBodyBox;

    [Header("기본 히트박스 (자세별)")]
    public List<BoxComponent> idleHurtBoxes;
    public List<BoxComponent> crouchHurtBoxes;
    public List<BoxComponent> jumpHurtBoxes;

    [Header("기본 윕퍼니시 박스 (자세별)")]
    public List<BoxComponent> idleWhiffBoxes;
    public List<BoxComponent> crouchWhiffBoxes;
    public List<BoxComponent> jumpWhiffBoxes;

    public bool isGuarding;
    public bool isJumping;
    public bool isSitting;
    public bool isFacingRight;
    public bool isSpecialPosing;
    public bool isAttacking;

    public Skill currentSkill;

    public LastHitInfo lastHitInfo;

    public List<Skill> idleSkills;
    public List<Skill> crouchSkills;
    public List<Skill> jumpSkills;
    public List<Skill> usableSkills = new();

    private PhysicsEntity physicsEntity;

    private void Start()
    {
        physicsEntity = GetComponent<PhysicsEntity>();
    }

    // 활성 상태에 따라 on/off 제어됨
    public void EnableDefaultBoxes(CharacterStateTag tag)
    {
        // 전부 끄기
        idleBodyBox.gameObject.SetActive(false);
        crouchBodyBox.gameObject.SetActive(false);
        jumpBodyBox.gameObject.SetActive(false);
        SetActiveAll(idleHurtBoxes, false);
        SetActiveAll(crouchHurtBoxes, false);
        SetActiveAll(jumpHurtBoxes, false);
        SetActiveAll(idleWhiffBoxes, false);
        SetActiveAll(crouchWhiffBoxes, false);
        SetActiveAll(jumpWhiffBoxes, false);

        // 해당 태그만 켜기 및 물리 엔티티 박스 갱신
        switch (tag)
        {
            case CharacterStateTag.Standing:
                idleBodyBox.gameObject.SetActive(true);
                SetActiveAll(idleHurtBoxes, true);
                physicsEntity.BodyBox = idleBodyBox;
                break;
            case CharacterStateTag.Crouching:
                crouchBodyBox.gameObject.SetActive(true);
                SetActiveAll(crouchHurtBoxes, true);
                physicsEntity.BodyBox = crouchBodyBox;
                break;
            case CharacterStateTag.Jumping:
                jumpBodyBox.gameObject.SetActive(true);
                SetActiveAll(jumpHurtBoxes, true);
                physicsEntity.BodyBox = jumpBodyBox;
                break;
            case CharacterStateTag.Down:
                crouchBodyBox.gameObject.SetActive(true);
                physicsEntity.BodyBox = crouchBodyBox;
                break;
        }
    }

    private void SetActiveAll(List<BoxComponent> boxes, bool active)
    {
        foreach (var box in boxes)
        {
            if (box != null)
                box.gameObject.SetActive(active);
        }
    }

    public void SetWhiffBox(bool active)
    {
        // 전부 끄기
        SetActiveAll(idleWhiffBoxes, false);
        SetActiveAll(crouchWhiffBoxes, false);
        SetActiveAll(jumpWhiffBoxes, false);

        // 현재 자세 따라 세팅
        if (isSitting)
        {
            SetActiveAll(crouchWhiffBoxes, active);
        }
        else if (isJumping)
        {
            SetActiveAll(jumpWhiffBoxes, active);
        }
        else
        {
            SetActiveAll(idleWhiffBoxes, active);
        }
    }

    public void UpdateUsableSkills(List<Skill> skills)
    {
        usableSkills.Clear();
        usableSkills.AddRange(skills);
    }
}
