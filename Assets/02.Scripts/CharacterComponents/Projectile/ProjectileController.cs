using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(PhysicsEntity))]
[RequireComponent(typeof(CharacterProperty))]
[RequireComponent(typeof(CollisionResolver))]
public class ProjectileController : MonoBehaviour, ITicker
{
    [Header("Multi-Hit")]
    [SerializeField] int maxHits = 3;            // 이 투사체의 총 히트 허용 수
    [SerializeField] int rehitLockFrames = 6;    // 같은 대상에게 재히트 허용까지 기다릴 프레임
    [SerializeField] bool destroyOnHit = false;  // 다단히트 장풍이면 보통 false
    [SerializeField] bool destroyOnBlock = false;

    int _hitsDone;
    readonly Dictionary<CharacterProperty, int> _rehitLock = new();

    public CharacterProperty OwnerProp { get; private set; }  // Init 때 세팅
    CharacterProperty prop;       // this projectile's prop
    PhysicsEntity phys;

    // runtime 상태
    private Vector2 velocity;
    private float gravityScale;
    private float remainLife;

    // 현재 프레임 카운터(애니 없이 Tick 기반)
    private int _frame;

    // 활성화된 박스(= Skill_SO.boxLifetimes의 인덱스 → BoxComponent)
    private readonly Dictionary<int, BoxComponent> _activeBoxes = new();

    private static int s_attackSeed = 1;

    public void Awake()
    {
        prop = GetComponent<CharacterProperty>();
        phys = GetComponent<PhysicsEntity>();
    }

    public void Init(CharacterProperty owner, ProjectileSpawnEvent cfg, bool ownerFacingRight, Skill_SO fallbackSkill)
    {
        _frame = 0;

        OwnerProp = owner;

        // 투사체도 “캐릭터처럼” 취급
        prop.currentSkill = cfg.projectileSkill != null ? cfg.projectileSkill : fallbackSkill;
        prop.isProjectileInvincible = false; // 필요시 활용
        // (선택) prop에 isProjectile 플래그 추가해도 좋음

        prop.attackInstanceId = CharacterProperty.NextAttackInstanceId();

        // 페이싱 기준 초기 속도
        float sx = ownerFacingRight ? +1f : -1f;
        velocity = new Vector2(cfg.initialVelocity.x * sx, cfg.initialVelocity.y);
        gravityScale = cfg.gravityScale;
        remainLife = cfg.lifeTimeSec;
        destroyOnHit = cfg.destroyOnHit;

        // 물리 모드
        phys.mode = PhysicsMode.Normal;
        phys.isGravityOn = (gravityScale != 0);

        prop.isFacingRight = (velocity.x >= 0f);

        // 틱 등록
        TickMaster.Instance.Register(this);
    }

    public void Tick()
    {
        float dt = TickMaster.TICK_INTERVAL;

        // 간단 이동(넉백/충돌은 기존 CollisionResolver/BoxManager로 처리)
        if (phys.isGravityOn)
            velocity.y += Physics2D.gravity.y * gravityScale * dt;

        phys.Position += velocity * dt;

        if (_rehitLock.Count > 0)
        {
            var keys = _rehitLock.Keys.ToArray();
            for (int i = 0; i < keys.Length; ++i)
            {
                _rehitLock[keys[i]]--;
                if (_rehitLock[keys[i]] <= 0) _rehitLock.Remove(keys[i]);
            }
        }

        SyncSkillBoxes();

        // 수명
        if (remainLife > 1e-6f)
        {
            remainLife -= dt;
            if (remainLife <= 0f)
            {
                Despawn();
                return;
            }
        }

        prop.isFacingRight = (velocity.x >= 0f);

        _frame++;
    }

    public bool CanHit(CharacterProperty target)
        => !_rehitLock.ContainsKey(target);

    public void OnDealtHit(CharacterProperty target)
    {
        _hitsDone++;
        _rehitLock[target] = rehitLockFrames;

        // (옵션) 히트마다 새로운 공격 인스턴스로 취급해 히트스탑 1회씩 주고 싶다면:
        prop.attackInstanceId = CharacterProperty.NextAttackInstanceId();

        if (destroyOnHit || (_hitsDone >= maxHits))
            Despawn();
    }

    public void OnBlocked(CharacterProperty target)
    {
        _rehitLock[target] = rehitLockFrames;
        if (destroyOnBlock) Despawn();
    }

    public void Despawn()
    {
        TickMaster.Instance.Unregister(this);
        ClearAllBoxes();

        // (선택) 풀링 사용 시 풀로 반환
        Destroy(gameObject);
    }

    // ---- 히트/블록 등 이벤트로 파괴(옵션) ----
    // CollisionResolver가 공격자/피격자 CharacterProperty를 참조하므로
    // “이 투사체가 공격자”로 히트를 냈을 때 파괴시키려면,
    // 간단히 아래 public API를 CollisionResolver 쪽에서 호출하도록 한 줄 넣으면 됨.

    public void OnOwnerDealtHit()
    {
        if (destroyOnHit) Despawn();
    }

    /// <summary>
    /// AnimationPlayer 없이 Skill_SO.boxLifetimes를 Tick 프레임(_frame) 기준으로 켜고/끄는 동기화
    /// </summary>
    private void SyncSkillBoxes()
    {
        var skill = prop != null ? prop.currentSkill : null;
        if (skill == null || skill.boxLifetimes == null) return;

        // 이번 프레임에 "활성이어야 하는" 수명 인덱스 수집
        List<int> wanted = null;
        int count = skill.boxLifetimes.Count;
        for (int i = 0; i < count; i++)
        {
            var life = skill.boxLifetimes[i];
            if (_frame >= life.startFrame && _frame <= life.endFrame)
            {
                (wanted ??= new List<int>(8)).Add(i);
            }
        }

        // 1) 불필요해진 박스 제거
        if (_activeBoxes.Count > 0)
        {
            if (wanted == null || wanted.Count == 0)
            {
                ClearAllBoxes();
            }
            else
            {
                // O(활성수)로 돌리기 위해 해시셋 사용
                var keep = new HashSet<int>(wanted);
                // ToArray로 복사해두고 순회 중 삭제
                foreach (var kv in _activeBoxes.ToArray())
                {
                    if (!keep.Contains(kv.Key))
                        DespawnBox(kv.Key);
                }
            }
        }

        // 2) 필요한데 아직 없는 박스 스폰
        if (wanted != null)
        {
            for (int j = 0; j < wanted.Count; j++)
            {
                int idx = wanted[j];
                if (!_activeBoxes.ContainsKey(idx))
                    SpawnBox(skill, idx);
            }
        }
    }

    private void SpawnBox(Skill_SO skill, int lifeIndex)
    {
        var life = skill.boxLifetimes[lifeIndex];

        // life.incrementAttackInstance가 true면 attackInstanceId 증가
        if (life.incrementAttackInstance && prop != null)
            prop.attackInstanceId++;

        // 새 GameObject + BoxComponent
        var go = new GameObject($"Box_{life.type}");
        go.transform.SetParent(transform, false);

        var bc = go.AddComponent<BoxComponent>();
        bc.size = life.box.size;
        bc.type = life.type;
        bc.offset = life.box.center;
        bc.owner = phys;                // PhysicsEntity
        bc.sourceSkill = skill;

        bc.uid = ComputeStableUid_ForProjectile(lifeIndex);

        _activeBoxes[lifeIndex] = bc;
        BoxManager.Instance.Register(bc);
    }

    private void DespawnBox(int lifeIndex)
    {
        if (_activeBoxes.TryGetValue(lifeIndex, out var bc))
        {
            // BoxManager엔 Unregister가 없으니 리스트에서 제거 + 오브젝트 파괴
            BoxManager.Instance.activeBoxes.Remove(bc);
            if (bc != null) Destroy(bc.gameObject);
            _activeBoxes.Remove(lifeIndex);
        }
    }

    private void ClearAllBoxes()
    {
        if (_activeBoxes.Count == 0) return;
        foreach (var kv in _activeBoxes)
        {
            var bc = kv.Value;
            BoxManager.Instance.activeBoxes.Remove(bc);
            if (bc != null) Destroy(bc.gameObject);
        }
        _activeBoxes.Clear();
    }

    private int ComputeStableUid_ForProjectile(int lifeIndex)
    {
        // BoxPresetApplier.ComputeStableUid와 동일한 해시 규칙 유지
        unchecked
        {
            int h = phys != null ? phys.GetInstanceID() : 0;
            h = (h * 397) ^ (prop != null && prop.currentSkill != null ? prop.currentSkill.GetInstanceID() : 0);
            h = (h * 397) ^ lifeIndex;
            return h;
        }
    }
}
