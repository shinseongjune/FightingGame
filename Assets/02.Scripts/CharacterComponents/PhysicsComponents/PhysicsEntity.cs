using System.Collections.Generic;
using Unity.Hierarchy;
using UnityEngine;

public enum PhysicsMode
{
    Normal,     // 중력+이동 처리 O
    Kinematic,  // 중력/가속 무시, 외부가 위치를 정함(잡힌 상태 등)
    Carried     // 특정 타겟을 따라감(공격자 손 위치 등)
}

public class PhysicsEntity : MonoBehaviour
{
    private PhysicsManager _pm;
    private BoxManager _bm;

    [System.Serializable] public struct BoxNum { public Vector2 center; public Vector2 size; }

    [Header("Default Box Numbers")]
    public BoxNum idleBody, crouchBody, jumpBody, downBody;
    public List<BoxNum> idleHurts, crouchHurts, jumpHurts;
    public List<BoxNum> idleWhiffs, crouchWhiffs, jumpWhiffs;

    public CharacterProperty property;

    public Vector2 Position;
    public Vector2 Velocity;
    public bool isGravityOn = true;
    public bool isGrounded = false;
    public float groundY = 0f;

    // 현재 활성 bodybox
    public BoxComponent currentBodyBox;
    // 현재 활성 hurtbox
    public List<BoxComponent> currentHurtBoxes;
    // 현재 활성 wiffbox
    public List<BoxComponent> currentWhiffBoxes;


    // 자세별 바디박스 프리셋
    public BoxComponent idleBodyBox;
    public List<BoxComponent> idleHurtBoxes;
    public List<BoxComponent> idleWhiffBoxes;

    public BoxComponent crouchBodyBox;
    public List<BoxComponent> crouchHurtBoxes;
    public List<BoxComponent> crouchWhiffBoxes;

    public BoxComponent jumpBodyBox;
    public List<BoxComponent> jumpHurtBoxes;
    public List<BoxComponent> jumpWhiffBoxes;

    public BoxComponent downBodyBox;

    // 물리 처리 모드
    public PhysicsMode mode = PhysicsMode.Normal;

    // 충돌/히트 수신 토글
    public bool collisionsEnabled = true; // (BoxManager에서 owner 단위 필터)
    public bool receiveHits = true;       // (Hurt만 꺼야 할 때)
    public bool pushboxEnabled = true;    // (몸통 밀치기/겹침해소에 쓸 경우)
    public bool immovablePushbox = false; // 벽용

    // Carried 모드용
    public PhysicsEntity followTarget;
    public Vector2 followOffset;

    // 내부: 이전에 등록했던 기본 허트박스 추적용
    private readonly List<BoxComponent> _registeredDefaultHurt = new();

    void Awake()
    {
        property = GetComponent<CharacterProperty>();

        BuildDefaultBoxesFromNumbers();
    }
    void OnEnable()
    {
        Position = (Vector2)transform.position;
        isGrounded = Position.y <= groundY + 1e-3f;
        
        _pm = PhysicsManager.Instance;
        _pm?.Register(this);

        _bm = BoxManager.Instance;
    }

    void OnDisable()
    {
        _pm?.Unregister(this);
        // 기본 허트박스 등록 해제(안전)
        UnregisterDefaultHurt();
    }

    public void BuildDefaultBoxesFromNumbers()
    {
        // Body들
        if (idleBody.size != Vector2.zero) idleBodyBox = MakeBox("Body_Idle", BoxType.Body, idleBody);
        if (crouchBody.size != Vector2.zero) crouchBodyBox = MakeBox("Body_Crouch", BoxType.Body, crouchBody);
        if (jumpBody.size != Vector2.zero) jumpBodyBox = MakeBox("Body_Jump", BoxType.Body, jumpBody);
        if (downBody.size != Vector2.zero) downBodyBox = MakeBox("Body_Down", BoxType.Body, downBody);

        // Hurt/Whiff 목록
        idleHurtBoxes = MakeList("Hurt_Idle", BoxType.Hurt, idleHurts);
        crouchHurtBoxes = MakeList("Hurt_Crouch", BoxType.Hurt, crouchHurts);
        jumpHurtBoxes = MakeList("Hurt_Jump", BoxType.Hurt, jumpHurts);

        idleWhiffBoxes = MakeList("Whiff_Idle", BoxType.Hit, idleWhiffs);
        crouchWhiffBoxes = MakeList("Whiff_Crouch", BoxType.Hit, crouchWhiffs);
        jumpWhiffBoxes = MakeList("Whiff_Jump", BoxType.Hit, jumpWhiffs);
    }

    BoxComponent MakeBox(string name, BoxType type, BoxNum num)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var bc = go.AddComponent<BoxComponent>();
        bc.owner = this; bc.type = type; bc.offset = num.center; bc.size = num.size;
        return bc;
    }

    List<BoxComponent> MakeList(string prefix, BoxType type, List<BoxNum> nums)
    {
        var list = new List<BoxComponent>();
        if (nums == null) return list;
        for (int i = 0; i < nums.Count; i++) list.Add(MakeBox($"{prefix}_{i}", type, nums[i]));
        return list;
    }

    public void SetPose(CharacterStateTag state)
    {
        switch (state)
        {
            case CharacterStateTag.Idle:
                currentBodyBox = idleBodyBox;
                currentWhiffBoxes = idleWhiffBoxes;
                currentHurtBoxes = idleHurtBoxes;
                break;

            case CharacterStateTag.Crouch:
                currentBodyBox = crouchBodyBox;
                currentWhiffBoxes = crouchWhiffBoxes;
                currentHurtBoxes = crouchHurtBoxes;
                break;

            case CharacterStateTag.Jump_Up:
            case CharacterStateTag.Jump_Forward:
            case CharacterStateTag.Jump_Backward:
                currentBodyBox = jumpBodyBox;
                currentWhiffBoxes = jumpWhiffBoxes;
                currentHurtBoxes = jumpHurtBoxes;
                break;

            case CharacterStateTag.Knockdown:
            case CharacterStateTag.HardKnockdown:
                currentBodyBox = downBodyBox;
                currentWhiffBoxes = null;
                currentHurtBoxes = null;
                break;
        }

        // 자세가 바뀔 때마다 기본 허트박스 세트를 새로 적용
        ApplyDefaultBoxes();
    }

    public void SyncTransform() => transform.position = Position;

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

    // --------- 내부 도우미 ---------

    void ApplyDefaultBoxes()
    {
        // 1) 이전 허트박스 해제
        UnregisterDefaultHurt();

        // 2) 새 허트박스 세트 등록
        if (currentHurtBoxes != null)
        {
            foreach (var hb in currentHurtBoxes)
            {
                if (hb == null) continue;

                // 타입 보정(프리팹에서 틀렸을 수 있으니 안전빵)
                hb.type = BoxType.Hurt;

                // owner 보정
                if (hb.owner == null) hb.owner = this;

                // BoxManager에 등록
                BoxManager.Instance?.Register(hb);
                _registeredDefaultHurt.Add(hb);
            }
        }

        // 3) 바디박스는 기본적으로 충돌 밀치기용이거나 디버그용이라
        //    필요 시 여기서 등록/관리 가능. (현재 시스템에선 AABB 충돌엔 Body 미사용)
        if (currentBodyBox != null)
        {
            if (currentBodyBox.owner == null) currentBodyBox.owner = this;
        }
    }

    public void SetActiveWhiffBoxes(bool active)
    {
        if (currentWhiffBoxes != null)
        {
            foreach (var hb in currentWhiffBoxes)
            {
                if (hb == null) continue;

                hb.type = BoxType.Hurt;

                if (hb.owner == null) hb.owner = this;

                if (active)
                {
                    BoxManager.Instance?.Register(hb);
                }
                else
                {
                    BoxManager.Instance.Unregister(hb);
                }
            }
        }
    }

    void UnregisterDefaultHurt()
    {
        if (_registeredDefaultHurt.Count == 0) return;
        for (int i = _registeredDefaultHurt.Count - 1; i >= 0; i--)
        {
            var hb = _registeredDefaultHurt[i];
            if (hb != null)
               _bm?.Unregister(hb);
        }
        _registeredDefaultHurt.Clear();
    }
}