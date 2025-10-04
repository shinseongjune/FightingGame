using UnityEngine;

public sealed class CameraRig_25D : MonoBehaviour
{
    [Header("References")]
    public Camera charCam;     // Orthographic (Characters)
    public Camera bgCam;       // Perspective (Background)
    public Transform[] fighters = new Transform[2]; // P1, P2 (�� �� null ���)

    [Header("Framing")]
    [Tooltip("�� ĳ���� ���� ���� �е� (���� �ݳ��� ���� �߰���)")]
    public float padding = 1.5f;
    [Tooltip("���翵 ī�޶� �ּ� �ݳ���(���� �Ѱ�)")]
    public float minOrthoSize = 1f;
    [Tooltip("���翵 ī�޶� �ִ� �ݳ���(�ܾƿ� �Ѱ�)")]
    public float maxOrthoSize = 9.0f;
    [Tooltip("ī�޶� �߽��� Y ������ (ĳ���� �߹ٴ� �����̸� 1~1.5 ��õ)")]
    public float heightOffset = 1.2f;

    [Header("Smoothing")]
    public float posSmoothTime = 0.12f;
    public float sizeSmoothTime = 0.12f;

    [Header("Background/Character Camera")]
    [Tooltip("BgCam�� ĳ���� ���(Z=targetPlaneZ)���� ������ �Ÿ�(���). �� ���� ���� FOV�� �ڵ� ����.")]
    public float charCamDistance = 10f;   // ĳ���� ������κ����� �Ÿ�(���). ���� z�� targetPlaneZ - �� ��
    public float bgCamDistance = 12f;
    [Tooltip("ĳ���͵��� �ο�� ����� Z��(2.5D��� �밳 0).")]
    public float targetPlaneZ = 0f;

    [Header("Stage Extents (use these or stageBounds)")]
    public bool useNumericStageExtents = true;
    public float leftX = -8f;
    public float rightX = 8f;
    public float groundY = 0f;
    public float ceilingY = 6f;

    [Header("Framing Bias / Margins")]
    public float bottomExtra = 0.6f; // �ٴ� �� ���̱�(���� ����)

    [Header("Stage Bounds (Optional)")]
    public BoxCollider stageBounds; // ������ ī�޶� �߽��� �� �ڽ� ������ Ŭ����

    [Header("Clamp Softness")]
    [Range(0f, 2f)] public float verticalSoftZone = 0.8f; // ���� ���� ����

    [Header("Zoom Tuning")]
    [Range(0.5f, 1.0f)] public float zoomBias = 0.85f; // 1���� ������ �� ������

    [Header("Behavior")]
    public bool preferZoomWhenTopClamped = true;

    // internals
    private Vector3 _vel;       // SmoothDamp��
    private float _sizeVel;     // SmoothDamp��

    // �ܺο��� 1������ġ�� ���ԵǴ� ��鸲 ��(���� XY, z-��)
    private Vector2 _externalOffset;
    private float _externalRollDeg;

    /// <summary>CameraShake �� �ܺ� FX�� ���� �����ӿ� ������ ���� ������/���� ���</summary>
    public void ApplyExternalShake(Vector2 worldOffset, float rollDeg)
    {
        _externalOffset += worldOffset;
        _externalRollDeg += rollDeg;
    }

    void Reset()
    {
        // �ڵ� ����
        charCam = GetComponentInChildren<Camera>();
        if (charCam != null && charCam.orthographic == false)
        {
            // ���� ù ī�޶� BgCam�̸� �ݴ�Ρ�
        }
    }

    void LateUpdate()
    {
        if (charCam == null || bgCam == null)
            return;

        // 1) �� �������� ��ȿ�� ��ġ�� ����
        Vector3? p1 = (fighters.Length > 0 && fighters[0] != null) ? fighters[0].position : (Vector3?)null;
        Vector3? p2 = (fighters.Length > 1 && fighters[1] != null) ? fighters[1].position : (Vector3?)null;

        if (p1 == null && p2 == null)
        {
            charCam.enabled = false;
            bgCam.enabled = false;
            return;
        }
        else
        {
            charCam.enabled = true;
            bgCam.enabled = true;
        }

        // 2) �߽����� �Ÿ� ��� (2D ����: X�� ����, Y�� ����, Z�� �������)
        Vector3 a = p1 ?? p2.Value;
        Vector3 b = p2 ?? p1.Value;

        float dx = Mathf.Abs(b.x - a.x);
        float dy = Mathf.Abs(b.y - a.y);

        // ���� ���ַ� ���, ������ �ణ ���(ĳ���� ���� �� ȭ���� �ʹ� Ÿ��Ʈ���� �ʵ���)
        float targetHalfHeight = Mathf.Max(dx * 0.5f, dy * 0.6f) + padding;
        targetHalfHeight = Mathf.Clamp(targetHalfHeight, minOrthoSize, maxOrthoSize);
        targetHalfHeight *= zoomBias;

        Vector3 targetCenter = new Vector3(
            (a.x + b.x) * 0.5f,
            ((a.y + b.y) * 0.5f) + heightOffset,
            targetPlaneZ
        );

        // 0) �ػ�-�������� �Ѱ迡�� ��� ������ �ִ� �ݳ��� ���
        float hMaxByStage = Mathf.Infinity;
        if (useNumericStageExtents)
        {
            float hLimitY = (ceilingY - groundY) * 0.5f;
            float hLimitX = (rightX - leftX) * 0.5f / Mathf.Max(0.0001f, charCam.aspect);
            hMaxByStage = Mathf.Min(hLimitY, hLimitX);
        }
        else if (stageBounds != null)
        {
            Bounds sb = stageBounds.bounds;
            float hLimitY = sb.size.y * 0.5f;
            float hLimitX = (sb.size.x * 0.5f) / Mathf.Max(0.0001f, charCam.aspect);
            hMaxByStage = Mathf.Min(hLimitY, hLimitX);
        }

        // 1) ��ǥ �ݳ��� ���� (�������� ���ġ, min/max �� ��� �ݿ�)
        targetHalfHeight = Mathf.Min(targetHalfHeight, hMaxByStage);
        targetHalfHeight = Mathf.Clamp(targetHalfHeight, minOrthoSize, maxOrthoSize);

        // 2) ��ǥ �߽��� ȭ�� �ݳʺ�/�ݳ��� �������� �������� �ȿ� Ŭ����
        if (preferZoomWhenTopClamped)
        {
            // ���� unclamped center �������� ��� ��ħ ����
            float predictedHalfH = targetHalfHeight;
            float predictedMaxY = ceilingY - predictedHalfH;
            if (targetCenter.y > predictedMaxY)
            {
                // �� ��� ���� halfHeight�� Ű������ (�������� ���ġ �ȿ���)
                float needed = targetCenter.y - predictedMaxY;
                float grow = needed; // ���������� 1:1�� �÷��� OK
                targetHalfHeight = Mathf.Min(targetHalfHeight + grow, hMaxByStage);
            }
        }

        float halfH = targetHalfHeight;
        float halfW = halfH * charCam.aspect;
        
        if (useNumericStageExtents)
        {
            targetCenter.x = Mathf.Clamp(targetCenter.x, leftX + halfW, rightX - halfW);

            float minY = groundY + halfH - bottomExtra;   // �ٴ� ������ŭ �� ������ �� �ְ�
            float maxY = ceilingY - halfH;

            targetCenter.y = Mathf.Clamp(targetCenter.y, minY, maxY);

            float unclampedY = targetCenter.y;
            float clampedY = Mathf.Clamp(unclampedY, minY, maxY);

            if (unclampedY > maxY)
            {
                // ���� �������� ���� ������ŭ�� ������ ����
                targetCenter.y = Mathf.Lerp(unclampedY, maxY, Mathf.InverseLerp(0f, verticalSoftZone, unclampedY - maxY));
            }
            else if (unclampedY < minY)
            {
                // �Ʒ��� ������ ���� ����
                targetCenter.y = Mathf.Lerp(unclampedY, minY, Mathf.InverseLerp(0f, verticalSoftZone, minY - unclampedY));
            }
            else
            {
                targetCenter.y = clampedY; // �� ��ġ�� �״��
            }
        }
        else if (stageBounds != null)
        {
            Bounds sb = stageBounds.bounds;
            float minX = sb.min.x + halfW;
            float maxX = sb.max.x - halfW;
            float minY = sb.min.y + halfH;
            float maxY = sb.max.y - halfH;

            targetCenter.x = Mathf.Clamp(targetCenter.x, minX, maxX);
            targetCenter.y = Mathf.Clamp(targetCenter.y, minY, maxY);
        }

        // 3) ������ ����
        Vector3 currentPos = charCam.transform.position;
        Vector3 desiredPos = new Vector3(targetCenter.x, targetCenter.y, targetPlaneZ - charCamDistance);
        Vector3 smoothed = Vector3.SmoothDamp(charCam.transform.position, desiredPos, ref _vel, posSmoothTime);
        charCam.transform.position = smoothed;

        float smoothedSize = Mathf.SmoothDamp(charCam.orthographicSize, targetHalfHeight, ref _sizeVel, sizeSmoothTime);
        charCam.orthographicSize = smoothedSize;

        SyncBackgroundCamera(smoothedSize);

        // �ܺ� ��鸲 ������/�� ����
        if (charCam != null)
        {
            var t = charCam.transform;
            t.position += new Vector3(_externalOffset.x, _externalOffset.y, 0f);
            t.rotation = Quaternion.Euler(0f, 0f, _externalRollDeg) * t.rotation;
        }
        if (bgCam != null)
        {
            var t = bgCam.transform;
            // ��浵 ���� �����̵��� �����ؾ� �� ī�޶� ������ ������
            t.position += new Vector3(_externalOffset.x, _externalOffset.y, 0f);
            t.rotation = Quaternion.Euler(0f, 0f, _externalRollDeg) * t.rotation;
        }

        // 4) �� ������ ��� �� �ʱ�ȭ(���� ����)
        _externalOffset = Vector2.zero;
        _externalRollDeg = 0f;
    }

    void SyncBackgroundCamera(float orthoHalfHeight)
    {
        // CharCam�� ���� x,y, ȸ��, �� z�� �� �ָ�
        bgCam.transform.position = new Vector3(
            charCam.transform.position.x,
            charCam.transform.position.y,
            targetPlaneZ - bgCamDistance
        );
        bgCam.transform.rotation = charCam.transform.rotation;

        // ���� �����̹� ��ġ: FOV = 2*atan(h / D), D�� ���� ���� BgCam ���� �Ÿ�
        float h = Mathf.Max(0.0001f, orthoHalfHeight);
        float D = Mathf.Max(0.0001f, Mathf.Abs(bgCamDistance));
        float vfovRad = 2f * Mathf.Atan(h / D);
        bgCam.fieldOfView = Mathf.Clamp(vfovRad * Mathf.Rad2Deg, 1f, 179f);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // ī�޶� ���� �ݳ��� �ð�ȭ
        if (charCam != null)
        {
            Gizmos.color = Color.cyan;
            float h = charCam.orthographicSize;
            float w = h * charCam.aspect;
            Vector3 c = new Vector3(charCam.transform.position.x, charCam.transform.position.y, targetPlaneZ);
            Gizmos.DrawWireCube(c, new Vector3(w * 2f, h * 2f, 0.01f));
        }

        if (stageBounds != null)
        {
            Gizmos.color = new Color(1, 0.7f, 0.2f, 0.8f);
            Gizmos.DrawWireCube(stageBounds.bounds.center, stageBounds.bounds.size);
        }
    }
#endif
}
