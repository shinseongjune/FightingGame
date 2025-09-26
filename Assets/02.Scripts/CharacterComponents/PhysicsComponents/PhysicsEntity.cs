using System.Collections.Generic;
using Unity.Hierarchy;
using UnityEngine;

public enum PhysicsMode
{
    Normal,     // �߷�+�̵� ó�� O
    Kinematic,  // �߷�/���� ����, �ܺΰ� ��ġ�� ����(���� ���� ��)
    Carried     // Ư�� Ÿ���� ����(������ �� ��ġ ��)
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

    // ���� Ȱ�� bodybox
    public BoxComponent currentBodyBox;
    // ���� Ȱ�� hurtbox
    public List<BoxComponent> currentHurtBoxes;
    // ���� Ȱ�� wiffbox
    public List<BoxComponent> currentWhiffBoxes;


    // �ڼ��� �ٵ�ڽ� ������
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

    // ���� ó�� ���
    public PhysicsMode mode = PhysicsMode.Normal;

    // �浹/��Ʈ ���� ���
    public bool collisionsEnabled = true; // (BoxManager���� owner ���� ����)
    public bool receiveHits = true;       // (Hurt�� ���� �� ��)
    public bool pushboxEnabled = true;    // (���� ��ġ��/��ħ�ؼҿ� �� ���)
    public bool immovablePushbox = false; // ����

    // Carried ����
    public PhysicsEntity followTarget;
    public Vector2 followOffset;

    // ����: ������ ����ߴ� �⺻ ��Ʈ�ڽ� ������
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
        // �⺻ ��Ʈ�ڽ� ��� ����(����)
        UnregisterDefaultHurt();
    }

    public void BuildDefaultBoxesFromNumbers()
    {
        // Body��
        if (idleBody.size != Vector2.zero) idleBodyBox = MakeBox("Body_Idle", BoxType.Body, idleBody);
        if (crouchBody.size != Vector2.zero) crouchBodyBox = MakeBox("Body_Crouch", BoxType.Body, crouchBody);
        if (jumpBody.size != Vector2.zero) jumpBodyBox = MakeBox("Body_Jump", BoxType.Body, jumpBody);
        if (downBody.size != Vector2.zero) downBodyBox = MakeBox("Body_Down", BoxType.Body, downBody);

        // Hurt/Whiff ���
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

        // �ڼ��� �ٲ� ������ �⺻ ��Ʈ�ڽ� ��Ʈ�� ���� ����
        ApplyDefaultBoxes();
    }

    public void SyncTransform() => transform.position = Position;

    // ���� API
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
        collisionsEnabled = false; // �ʿ� �� �ǰ�/��ġ�� ��� ��Ȱ��
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

    // --------- ���� ����� ---------

    void ApplyDefaultBoxes()
    {
        // 1) ���� ��Ʈ�ڽ� ����
        UnregisterDefaultHurt();

        // 2) �� ��Ʈ�ڽ� ��Ʈ ���
        if (currentHurtBoxes != null)
        {
            foreach (var hb in currentHurtBoxes)
            {
                if (hb == null) continue;

                // Ÿ�� ����(�����տ��� Ʋ���� �� ������ ������)
                hb.type = BoxType.Hurt;

                // owner ����
                if (hb.owner == null) hb.owner = this;

                // BoxManager�� ���
                BoxManager.Instance?.Register(hb);
                _registeredDefaultHurt.Add(hb);
            }
        }

        // 3) �ٵ�ڽ��� �⺻������ �浹 ��ġ����̰ų� ����׿��̶�
        //    �ʿ� �� ���⼭ ���/���� ����. (���� �ý��ۿ��� AABB �浹�� Body �̻��)
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