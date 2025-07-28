using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ������ ����
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
/// ���� ���� ����
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

    public Vector2 hitPoint;      // ���� ��ġ (AABB ��� ����)
    public HitDirection direction; // �ִϸ��̼� ����� ����
    public bool fromFront;        // ������ ���鿡�� �¾Ҵ��� (false�鼭 direction�� front�� ��� �ڿ��� ���� ������ ���)
    public HitRegion region;      // ��ü ���� �з�

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
{// �׻� �����ϴ� �ٵ� �ڽ� (������/�浹��)
    public BoxComponent idleBodyBox;
    public BoxComponent crouchBodyBox;
    public BoxComponent jumpBodyBox;

    [Header("�⺻ ��Ʈ�ڽ� (�ڼ���)")]
    public List<BoxComponent> idleHurtBoxes;
    public List<BoxComponent> crouchHurtBoxes;
    public List<BoxComponent> jumpHurtBoxes;

    [Header("�⺻ ���۴Ͻ� �ڽ� (�ڼ���)")]
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

    // Ȱ�� ���¿� ���� on/off �����
    public void EnableDefaultBoxes(CharacterStateTag tag)
    {
        // ���� ����
        idleBodyBox.gameObject.SetActive(false);
        crouchBodyBox.gameObject.SetActive(false);
        jumpBodyBox.gameObject.SetActive(false);
        SetActiveAll(idleHurtBoxes, false);
        SetActiveAll(crouchHurtBoxes, false);
        SetActiveAll(jumpHurtBoxes, false);
        SetActiveAll(idleWhiffBoxes, false);
        SetActiveAll(crouchWhiffBoxes, false);
        SetActiveAll(jumpWhiffBoxes, false);

        // �ش� �±׸� �ѱ� �� ���� ��ƼƼ �ڽ� ����
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
        // ���� ����
        SetActiveAll(idleWhiffBoxes, false);
        SetActiveAll(crouchWhiffBoxes, false);
        SetActiveAll(jumpWhiffBoxes, false);

        // ���� �ڼ� ���� ����
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
