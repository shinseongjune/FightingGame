using System.Collections.Generic;
using UnityEngine;

public enum CharacterStateTag
{
    None,
    Idle,
    Crouch,
    Jump_Up,
    Jump_Forward,
    Jump_Backward,
    Walk_Forward,
    Walk_Backward,
    Dash_Forward,
    Dash_Backward,
    Skill,
    Hit,
    Hit_Air,
    Hit_Guard,
    Guarding,
    Knockdown,
    HardKnockdown,
    DriveImpact,
    DriveRush,
    DriveParry,
    DriveReversal,
    Throw,
    BeingThrown,
    ForcedAnimation,
}

public class CharacterProperty : MonoBehaviour
{
    public CharacterStateTag characterStateTag;

    [Header("Skill Settings")]
    public List<Skill_SO> allSkills; // 캐릭터가 가진 전체 스킬
    public Skill_SO currentSkill;

    public readonly List<GameObject> projectiles;

    public bool isFacingRight;

    public bool isStartUp;
    public bool isRecovery;
    public bool isInputEnabled = true;
    public bool isSkillCancelable;
    public bool isInvincible;
    public bool isAirInvincible;
    public bool isProjectileInvincible;
    public int superArmorCount;

    public float hp;
    public float maxHp = 10000;

    public float saGauge;
    public float maxSAGauge = 300;

    public bool isExhausted;
    public bool isDriveGaugeCharging;
    public float driveGauge;
    public float maxDriveGauge = 600;
    public float driveGaugeTickChargeAmount = 1;

    public int pendingHitstunFrames;
    public int pendingBlockstunFrames;
    public Vector2 pendingKnockback;

    public void SetHitstun(int frames, Vector2 kb)
    {
        pendingHitstunFrames = frames;
        pendingKnockback = kb;
    }
    public void SetBlockstun(int frames)
    {
        pendingBlockstunFrames = frames;
    }

    public void ChargeSAGauge(float amount)
    {
        saGauge = Mathf.Min(saGauge + amount, maxSAGauge);
    }

    public void ChargeDriveGauge(float amount)
    {
        driveGauge = Mathf.Min(driveGauge + amount, maxDriveGauge);
    }

    public void SetFacing(bool facingRight)
    {
        if (isFacingRight == facingRight) return;
        isFacingRight = facingRight;

        if (facingRight)
        {
            transform.eulerAngles = new Vector3(0, 90, 0);
        }
        else
        {
            transform.eulerAngles = new Vector3(0, -90, 0);
        }
    }
}
