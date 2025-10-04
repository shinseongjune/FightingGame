using UnityEngine;

public sealed class CameraRig_25D : MonoBehaviour
{
    [Header("References")]
    public Camera charCam;     // Orthographic (Characters)
    public Camera bgCam;       // Perspective (Background)
    public Transform[] fighters = new Transform[2]; // P1, P2 (둘 중 null 허용)

    [Header("Framing")]
    [Tooltip("두 캐릭터 사이 여유 패딩 (세로 반높이 기준 추가값)")]
    public float padding = 1.5f;
    [Tooltip("정사영 카메라 최소 반높이(줌인 한계)")]
    public float minOrthoSize = 1f;
    [Tooltip("정사영 카메라 최대 반높이(줌아웃 한계)")]
    public float maxOrthoSize = 9.0f;
    [Tooltip("카메라 중심의 Y 오프셋 (캐릭터 발바닥 기준이면 1~1.5 추천)")]
    public float heightOffset = 1.2f;

    [Header("Smoothing")]
    public float posSmoothTime = 0.12f;
    public float sizeSmoothTime = 0.12f;

    [Header("Background/Character Camera")]
    [Tooltip("BgCam이 캐릭터 평면(Z=targetPlaneZ)에서 떨어진 거리(양수). 이 값에 따라 FOV가 자동 계산됨.")]
    public float charCamDistance = 10f;   // 캐릭터 평면으로부터의 거리(양수). 실제 z는 targetPlaneZ - 이 값
    public float bgCamDistance = 12f;
    [Tooltip("캐릭터들이 싸우는 평면의 Z값(2.5D라면 대개 0).")]
    public float targetPlaneZ = 0f;

    [Header("Stage Extents (use these or stageBounds)")]
    public bool useNumericStageExtents = true;
    public float leftX = -8f;
    public float rightX = 8f;
    public float groundY = 0f;
    public float ceilingY = 6f;

    [Header("Framing Bias / Margins")]
    public float bottomExtra = 0.6f; // 바닥 더 보이기(월드 단위)

    [Header("Stage Bounds (Optional)")]
    public BoxCollider stageBounds; // 있으면 카메라 중심을 이 박스 안으로 클램프

    [Header("Clamp Softness")]
    [Range(0f, 2f)] public float verticalSoftZone = 0.8f; // 월드 단위 완충

    [Header("Zoom Tuning")]
    [Range(0.5f, 1.0f)] public float zoomBias = 0.85f; // 1보다 작으면 더 가깝게

    [Header("Behavior")]
    public bool preferZoomWhenTopClamped = true;

    // internals
    private Vector3 _vel;       // SmoothDamp용
    private float _sizeVel;     // SmoothDamp용

    // 외부에서 1프레임치로 주입되는 흔들림 값(월드 XY, z-롤)
    private Vector2 _externalOffset;
    private float _externalRollDeg;

    /// <summary>CameraShake 등 외부 FX가 현재 프레임에 적용할 가외 오프셋/롤을 등록</summary>
    public void ApplyExternalShake(Vector2 worldOffset, float rollDeg)
    {
        _externalOffset += worldOffset;
        _externalRollDeg += rollDeg;
    }

    void Reset()
    {
        // 자동 추정
        charCam = GetComponentInChildren<Camera>();
        if (charCam != null && charCam.orthographic == false)
        {
            // 만약 첫 카메라가 BgCam이면 반대로…
        }
    }

    void LateUpdate()
    {
        if (charCam == null || bgCam == null)
            return;

        // 1) 두 파이터의 유효한 위치를 수집
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

        // 2) 중심점과 거리 계산 (2D 격투: X는 수평, Y는 수직, Z는 고정평면)
        Vector3 a = p1 ?? p2.Value;
        Vector3 b = p2 ?? p1.Value;

        float dx = Mathf.Abs(b.x - a.x);
        float dy = Mathf.Abs(b.y - a.y);

        // 수평 위주로 잡되, 수직도 약간 고려(캐릭터 점프 시 화면이 너무 타이트하지 않도록)
        float targetHalfHeight = Mathf.Max(dx * 0.5f, dy * 0.6f) + padding;
        targetHalfHeight = Mathf.Clamp(targetHalfHeight, minOrthoSize, maxOrthoSize);
        targetHalfHeight *= zoomBias;

        Vector3 targetCenter = new Vector3(
            (a.x + b.x) * 0.5f,
            ((a.y + b.y) * 0.5f) + heightOffset,
            targetPlaneZ
        );

        // 0) 해상도-스테이지 한계에서 허용 가능한 최대 반높이 계산
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

        // 1) 목표 반높이 제한 (스테이지 허용치, min/max 줌 모두 반영)
        targetHalfHeight = Mathf.Min(targetHalfHeight, hMaxByStage);
        targetHalfHeight = Mathf.Clamp(targetHalfHeight, minOrthoSize, maxOrthoSize);

        // 2) 목표 중심을 화면 반너비/반높이 기준으로 스테이지 안에 클램프
        if (preferZoomWhenTopClamped)
        {
            // 현재 unclamped center 기준으로 상단 넘침 예측
            float predictedHalfH = targetHalfHeight;
            float predictedMaxY = ceilingY - predictedHalfH;
            if (targetCenter.y > predictedMaxY)
            {
                // 더 담기 위해 halfHeight를 키워본다 (스테이지 허용치 안에서)
                float needed = targetCenter.y - predictedMaxY;
                float grow = needed; // 경험적으로 1:1로 올려도 OK
                targetHalfHeight = Mathf.Min(targetHalfHeight + grow, hMaxByStage);
            }
        }

        float halfH = targetHalfHeight;
        float halfW = halfH * charCam.aspect;
        
        if (useNumericStageExtents)
        {
            targetCenter.x = Mathf.Clamp(targetCenter.x, leftX + halfW, rightX - halfW);

            float minY = groundY + halfH - bottomExtra;   // 바닥 여유만큼 더 내려갈 수 있게
            float maxY = ceilingY - halfH;

            targetCenter.y = Mathf.Clamp(targetCenter.y, minY, maxY);

            float unclampedY = targetCenter.y;
            float clampedY = Mathf.Clamp(unclampedY, minY, maxY);

            if (unclampedY > maxY)
            {
                // 위로 넘쳤으면 완충 구간만큼만 서서히 당긴다
                targetCenter.y = Mathf.Lerp(unclampedY, maxY, Mathf.InverseLerp(0f, verticalSoftZone, unclampedY - maxY));
            }
            else if (unclampedY < minY)
            {
                // 아래로 넘쳤을 때도 동일
                targetCenter.y = Mathf.Lerp(unclampedY, minY, Mathf.InverseLerp(0f, verticalSoftZone, minY - unclampedY));
            }
            else
            {
                targetCenter.y = clampedY; // 안 넘치면 그대로
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

        // 3) 스무딩 적용
        Vector3 currentPos = charCam.transform.position;
        Vector3 desiredPos = new Vector3(targetCenter.x, targetCenter.y, targetPlaneZ - charCamDistance);
        Vector3 smoothed = Vector3.SmoothDamp(charCam.transform.position, desiredPos, ref _vel, posSmoothTime);
        charCam.transform.position = smoothed;

        float smoothedSize = Mathf.SmoothDamp(charCam.orthographicSize, targetHalfHeight, ref _sizeVel, sizeSmoothTime);
        charCam.orthographicSize = smoothedSize;

        SyncBackgroundCamera(smoothedSize);

        // 외부 흔들림 오프셋/롤 주입
        if (charCam != null)
        {
            var t = charCam.transform;
            t.position += new Vector3(_externalOffset.x, _externalOffset.y, 0f);
            t.rotation = Quaternion.Euler(0f, 0f, _externalRollDeg) * t.rotation;
        }
        if (bgCam != null)
        {
            var t = bgCam.transform;
            // 배경도 같은 평행이동을 적용해야 양 카메라 정렬이 유지됨
            t.position += new Vector3(_externalOffset.x, _externalOffset.y, 0f);
            t.rotation = Quaternion.Euler(0f, 0f, _externalRollDeg) * t.rotation;
        }

        // 4) 한 프레임 사용 후 초기화(누적 방지)
        _externalOffset = Vector2.zero;
        _externalRollDeg = 0f;
    }

    void SyncBackgroundCamera(float orthoHalfHeight)
    {
        // CharCam과 같은 x,y, 회전, 단 z만 더 멀리
        bgCam.transform.position = new Vector3(
            charCam.transform.position.x,
            charCam.transform.position.y,
            targetPlaneZ - bgCamDistance
        );
        bgCam.transform.rotation = charCam.transform.rotation;

        // 수직 프레이밍 일치: FOV = 2*atan(h / D), D는 전장 평면과 BgCam 사이 거리
        float h = Mathf.Max(0.0001f, orthoHalfHeight);
        float D = Mathf.Max(0.0001f, Mathf.Abs(bgCamDistance));
        float vfovRad = 2f * Mathf.Atan(h / D);
        bgCam.fieldOfView = Mathf.Clamp(vfovRad * Mathf.Rad2Deg, 1f, 179f);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // 카메라 수직 반높이 시각화
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
