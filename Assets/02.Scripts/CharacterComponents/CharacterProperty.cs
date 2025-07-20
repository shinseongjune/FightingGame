using System.Collections.Generic;
using UnityEngine;

public class CharacterProperty : MonoBehaviour
{
    public bool isGuarding;
    public bool isJumping;
    public bool isSitting;
    public bool isFacingRight;
    public bool isSpecialPosing;
    public bool isAttacking;

    public Skill currentSkill;

    public readonly List<Skill> usableSkills = new List<Skill>();
}
