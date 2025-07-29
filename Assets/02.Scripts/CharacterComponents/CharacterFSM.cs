using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public class CharacterFSM : MonoBehaviour, ITicker
{
    [Header("캐릭터 컴포넌트")]
    private CharacterProperty property;
    private SkillExecutor skillExecutor;
    private BoxPresetApplier boxPresetApplier;
    private AnimationPlayer animPlayer;
    private PhysicsEntity physicsEntity;

    private Dictionary<string, CharacterState> statePool = new();

    private CharacterState current;
    public CharacterState CurrentState => current;

    private void Start()
    {
        InitializeStates();

        property = GetComponent<CharacterProperty>();
        skillExecutor = GetComponent<SkillExecutor>();
        boxPresetApplier = GetComponent<BoxPresetApplier>();
        animPlayer = GetComponent<AnimationPlayer>();
        physicsEntity = GetComponent<PhysicsEntity>();
    }

    public void Tick()
    {
        current?.OnUpdate();
    }

    [Obsolete]
    public void TransitionTo(CharacterState next)
    {
        if (next == null) return;

        Debug.Log($"[FSM] {current?.GetType().Name} → {next.GetType().Name}");

        current?.OnExit();
        current = next;
        current?.OnEnter();
    }

    public void TransitionTo(string stateName)
    {
        if (!statePool.TryGetValue(stateName, out var next))
        {
            Debug.LogWarning($"[FSM] 상태 '{stateName}' 를 찾을 수 없습니다.");
            return;
        }

        if (current == next) return;

        Debug.Log($"[FSM] {current?.GetType().Name} → {next.GetType().Name}");

        current?.OnExit();
        current = next;
        current?.OnEnter();
    }

    public void InitializeStates()
    {
        //statePool["Idle"] = new IdleState(this);
        //statePool["Walk"] = new WalkState(this);
        //statePool["Skill"] = new SkillState(this);
        //statePool["HitStun"] = new HitStunState(this);
        // 임시. state들 개선 후 삽입할 것.
    }

    public void OnHit(CollisionEvent e)
    {
        var property = GetComponent<CharacterProperty>();

        // 1. 이미 같은 hitId에 피격됐는지 확인 (중복 방지)
        if (property.HasHitId(e.attackerBox.hitId))
            return;

        // 2. 무적, 슈퍼아머 등 상태 체크
        if (property.isInvincible ||
            property.invincibleType == InvincibleType.All ||
            (property.invincibleType == InvincibleType.AirAttack && e.attackerBox.isAirAttack))
        {
            Debug.Log("무적 상태로 피격 무시!");
            return;
        }

        // 3. 슈퍼아머
        if (property.superArmorCount > 0)
        {
            property.superArmorCount--;
            // 대미지 입음, 히트스턴 무시
            // ... (대미지 계산/적용)
            // (히트스탑/떨림/파티클 등 연출 가능)
            property.AddHitId(e.attackerBox.hitId);
            Debug.Log("슈퍼아머로 버팀! 남은 슈퍼아머: " + property.superArmorCount);
            return;
        }

        // 4. 실제 피격 판정
        // (상중하단 결정: 공중공격이면 위치판정, 아니면 고정값)
        HitRegion hitRegion = GetActualHitRegion(e.attackerBox, property, e);

        // 가드 가능 여부도 여기서 분기 가능!
        // if (CanGuard(hitRegion, property)) { ... }

        // 대미지, 상태 전이, 히트스탑 등 실제 적용
        property.AddHitId(e.attackerBox.hitId);
        property.comboCount++;
        // 상태 전이 등
        Debug.Log("피격! 상태 전이/연출/대미지 적용");
        // 예: 상태 전이 → this.TransitionTo("HitStun");
    }
    public void OnThrow(CollisionEvent e)
    {
        var property = GetComponent<CharacterProperty>();
        // 잡힘 처리: 즉시 잡힘 상태 전이, 대미지 등
        Debug.Log("잡힘! 상태 전이");
        // 예: this.TransitionTo("BeingThrown");
    }
    public void OnGuard(CollisionEvent e)
    {
        var property = GetComponent<CharacterProperty>();
        // 가드 성공 로직: 블록스턴, 연출, 상태 전이
        Debug.Log("가드 성공!");
        // 상태 전이 등
    }

    // 상중하단 판정 함수
    private HitRegion GetActualHitRegion(BoxComponent attackerBox, CharacterProperty target, CollisionEvent e)
    {
        if (attackerBox.isAirAttack)
            return DetermineRegion(e.attackerBox.WorldBounds.center, target);
        else
            return attackerBox.hitRegion;
    }

    private HitRegion DetermineRegion(Vector2 hitPoint, CharacterProperty target)
    {
        float y = hitPoint.y;
        float headY = target.headPoint.position.y;
        float bodyY = target.bodyPoint.position.y;
        float legsY = target.legsPoint.position.y;
        if (y > (headY + bodyY) / 2f) return HitRegion.Head;
        if (y < (bodyY + legsY) / 2f) return HitRegion.Legs;
        return HitRegion.Body;
    }
}
