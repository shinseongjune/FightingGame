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
    public BoxComponent bodyBox;

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

    // Ȱ�� ���¿� ���� on/off �����
    public void EnableDefaultBoxes(CharacterStateTag tag)
    {
        // ���� ����
        SetActiveAll(idleHurtBoxes, false);
        SetActiveAll(crouchHurtBoxes, false);
        SetActiveAll(jumpHurtBoxes, false);
        SetActiveAll(idleWhiffBoxes, false);
        SetActiveAll(crouchWhiffBoxes, false);
        SetActiveAll(jumpWhiffBoxes, false);

        // �ش� �±׸� �ѱ�
        switch (tag)
        {
            case CharacterStateTag.Standing:
                SetActiveAll(idleHurtBoxes, true);
                SetActiveAll(idleWhiffBoxes, true);
                break;
            case CharacterStateTag.Crouching:
                SetActiveAll(crouchHurtBoxes, true);
                SetActiveAll(crouchWhiffBoxes, true);
                break;
            case CharacterStateTag.Jumping:
                SetActiveAll(jumpHurtBoxes, true);
                SetActiveAll(jumpWhiffBoxes, true);
                break;
            case CharacterStateTag.Down:
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
}
