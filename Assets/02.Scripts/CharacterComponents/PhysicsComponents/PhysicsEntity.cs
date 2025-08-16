using System.Collections.Generic;
using UnityEngine;
public enum PhysicsMode
{
    Normal,     // �߷�+�̵� ó�� O
    Kinematic,  // �߷�/���� ����, �ܺΰ� ��ġ�� ����(���� ���� ��)
    Carried     // Ư�� Ÿ���� ����(������ �� ��ġ ��)
}

public class PhysicsEntity : MonoBehaviour
{
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

    // Carried ����
    public PhysicsEntity followTarget;
    public Vector2 followOffset;

    // ����: ������ ����ߴ� �⺻ ��Ʈ�ڽ� ������
    private readonly List<BoxComponent> _registeredDefaultHurt = new();

    void Awake()
    {
        // ĳ���� �����տ� �̸� �ڽ����� �ٿ��״ٸ� owner�� ����
        AssignOwnerIfMissing(idleBodyBox);
        AssignOwnerIfMissing(crouchBodyBox);
        AssignOwnerIfMissing(jumpBodyBox);
        AssignOwnerIfMissing(downBodyBox);
        AssignOwnerIfMissing(idleWhiffBoxes);
        AssignOwnerIfMissing(crouchWhiffBoxes);
        AssignOwnerIfMissing(jumpWhiffBoxes);
        AssignOwnerIfMissing(idleHurtBoxes);
        AssignOwnerIfMissing(crouchHurtBoxes);
        AssignOwnerIfMissing(jumpHurtBoxes);
    }

    void OnDisable()
    {
        // �⺻ ��Ʈ�ڽ� ��� ����(����)
        UnregisterDefaultHurt();
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

    void UnregisterDefaultHurt()
    {
        if (_registeredDefaultHurt.Count == 0) return;
        for (int i = _registeredDefaultHurt.Count - 1; i >= 0; i--)
        {
            var hb = _registeredDefaultHurt[i];
            if (hb != null)
                BoxManager.Instance?.Unregister(hb);
        }
        _registeredDefaultHurt.Clear();
    }

    void AssignOwnerIfMissing(BoxComponent box)
    {
        if (box != null && box.owner == null) box.owner = this;
    }

    void AssignOwnerIfMissing(List<BoxComponent> list)
    {
        if (list == null) return;
        foreach (var b in list)
            if (b != null && b.owner == null) b.owner = this;
    }
}