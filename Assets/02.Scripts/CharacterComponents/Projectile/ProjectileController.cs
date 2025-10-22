using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(PhysicsEntity))]
[RequireComponent(typeof(CharacterProperty))]
[RequireComponent(typeof(CollisionResolver))]
public class ProjectileController : MonoBehaviour, ITicker
{
    [Header("Multi-Hit")]
    [SerializeField] int maxHits = 3;            // �� ����ü�� �� ��Ʈ ��� ��
    [SerializeField] int rehitLockFrames = 6;    // ���� ��󿡰� ����Ʈ ������ ��ٸ� ������
    [SerializeField] bool destroyOnHit = false;  // �ٴ���Ʈ ��ǳ�̸� ���� false
    [SerializeField] bool destroyOnBlock = false;

    int _hitsDone;
    readonly Dictionary<CharacterProperty, int> _rehitLock = new();

    public CharacterProperty OwnerProp { get; private set; }  // Init �� ����
    CharacterProperty prop;       // this projectile's prop
    PhysicsEntity phys;

    // runtime ����
    private Vector2 velocity;
    private float gravityScale;
    private float remainLife;

    // ���� ������ ī����(�ִ� ���� Tick ���)
    private int _frame;

    // Ȱ��ȭ�� �ڽ�(= Skill_SO.boxLifetimes�� �ε��� �� BoxComponent)
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

        // ����ü�� ��ĳ����ó���� ���
        prop.currentSkill = cfg.projectileSkill != null ? cfg.projectileSkill : fallbackSkill;
        prop.isProjectileInvincible = false; // �ʿ�� Ȱ��
        // (����) prop�� isProjectile �÷��� �߰��ص� ����

        prop.attackInstanceId = CharacterProperty.NextAttackInstanceId();

        // ���̽� ���� �ʱ� �ӵ�
        float sx = ownerFacingRight ? +1f : -1f;
        velocity = new Vector2(cfg.initialVelocity.x * sx, cfg.initialVelocity.y);
        gravityScale = cfg.gravityScale;
        remainLife = cfg.lifeTimeSec;
        destroyOnHit = cfg.destroyOnHit;

        // ���� ���
        phys.mode = PhysicsMode.Normal;
        phys.isGravityOn = (gravityScale != 0);

        prop.isFacingRight = (velocity.x >= 0f);

        // ƽ ���
        TickMaster.Instance.Register(this);
    }

    public void Tick()
    {
        float dt = TickMaster.TICK_INTERVAL;

        // ���� �̵�(�˹�/�浹�� ���� CollisionResolver/BoxManager�� ó��)
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

        // ����
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

        // (�ɼ�) ��Ʈ���� ���ο� ���� �ν��Ͻ��� ����� ��Ʈ��ž 1ȸ�� �ְ� �ʹٸ�:
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

        // (����) Ǯ�� ��� �� Ǯ�� ��ȯ
        Destroy(gameObject);
    }

    // ---- ��Ʈ/��� �� �̺�Ʈ�� �ı�(�ɼ�) ----
    // CollisionResolver�� ������/�ǰ��� CharacterProperty�� �����ϹǷ�
    // ���� ����ü�� �����ڡ��� ��Ʈ�� ���� �� �ı���Ű����,
    // ������ �Ʒ� public API�� CollisionResolver �ʿ��� ȣ���ϵ��� �� �� ������ ��.

    public void OnOwnerDealtHit()
    {
        if (destroyOnHit) Despawn();
    }

    /// <summary>
    /// AnimationPlayer ���� Skill_SO.boxLifetimes�� Tick ������(_frame) �������� �Ѱ�/���� ����ȭ
    /// </summary>
    private void SyncSkillBoxes()
    {
        var skill = prop != null ? prop.currentSkill : null;
        if (skill == null || skill.boxLifetimes == null) return;

        // �̹� �����ӿ� "Ȱ���̾�� �ϴ�" ���� �ε��� ����
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

        // 1) ���ʿ����� �ڽ� ����
        if (_activeBoxes.Count > 0)
        {
            if (wanted == null || wanted.Count == 0)
            {
                ClearAllBoxes();
            }
            else
            {
                // O(Ȱ����)�� ������ ���� �ؽü� ���
                var keep = new HashSet<int>(wanted);
                // ToArray�� �����صΰ� ��ȸ �� ����
                foreach (var kv in _activeBoxes.ToArray())
                {
                    if (!keep.Contains(kv.Key))
                        DespawnBox(kv.Key);
                }
            }
        }

        // 2) �ʿ��ѵ� ���� ���� �ڽ� ����
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

        // life.incrementAttackInstance�� true�� attackInstanceId ����
        if (life.incrementAttackInstance && prop != null)
            prop.attackInstanceId++;

        // �� GameObject + BoxComponent
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
            // BoxManager�� Unregister�� ������ ����Ʈ���� ���� + ������Ʈ �ı�
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
        // BoxPresetApplier.ComputeStableUid�� ������ �ؽ� ��Ģ ����
        unchecked
        {
            int h = phys != null ? phys.GetInstanceID() : 0;
            h = (h * 397) ^ (prop != null && prop.currentSkill != null ? prop.currentSkill.GetInstanceID() : 0);
            h = (h * 397) ^ lifeIndex;
            return h;
        }
    }
}
