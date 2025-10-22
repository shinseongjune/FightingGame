using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class BoxDebugRendererGL : MonoBehaviour
{
    [Header("Toggle")]
    public Key toggleKey = Key.F9;
    public bool enabledRendering = true;

    [Header("Filter")]
    public bool preferBoxManager = true;   // 플레이 중엔 BoxManager 우선(스킬 히트박스 포함)
    public bool currentPoseOnly = true;    // 비플레이/프리팹/테스트씬에선 현재 포즈만
    public bool drawBody = true, drawHurt = true, drawHit = true, drawThrow = true, drawGuard = true;
    public bool includeWhiffAsHit = true;

    InputAction toggleAction;
    static Material lineMat;

    static bool s_Registered;

    void OnEnable()
    {
        if (!s_Registered)
        {
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
            s_Registered = true;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
            UnityEditor.EditorApplication.update += EditorUpdate;
        if (Application.isPlaying)
        {
            toggleAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/f9"); // F9 권장
            toggleAction.Enable();
        }
#endif
        EnsureMat();
        HookCamera(true);
        SetupInput();
    }
    void OnDisable()
    {
        if (s_Registered)
        {
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            s_Registered = false;
        }
#if UNITY_EDITOR
        if (!Application.isPlaying)
            UnityEditor.EditorApplication.update -= EditorUpdate;
        toggleAction?.Disable();
        toggleAction?.Dispose();
        toggleAction = null;
#endif
        HookCamera(false);
        CleanupInput();
    }

    void SetupInput()
    {
        if (toggleAction != null) return;
        // 새 Input System로 토글
        toggleAction = new InputAction(type: InputActionType.Button, binding: $"<Keyboard>/{toggleKey.ToString().ToLower()}");
        toggleAction.Enable();
    }
    void CleanupInput()
    {
        toggleAction?.Disable();
        toggleAction?.Dispose();
        toggleAction = null;
    }

    void Update()
    {
        if (!Application.isPlaying) return; // 에디터 모드에선 키 입력 아예 안 받음
        if (toggleAction != null && toggleAction.WasPerformedThisFrame())
            enabledRendering = !enabledRendering; // 또는 visible = !visible
    }
#if UNITY_EDITOR
    void EditorUpdate()
    {
        // 에디터 모드에서도 키 토글 허용(포커스가 GameView일 때)
        if (!Application.isPlaying && toggleAction != null && toggleAction.WasPerformedThisFrame())
            enabledRendering = !enabledRendering;

        // Scene/Game 뷰 리프레시
        UnityEditor.SceneView.RepaintAll();
        UnityEditor.EditorWindow game = UnityEditor.EditorWindow.GetWindow(typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.GameView"));
        game?.Repaint();
    }
#endif

    static void EnsureMat()
    {
        if (lineMat != null) return;
        var sh = Shader.Find("Hidden/Internal-Colored");
        lineMat = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };
        // 화면 위에 항상 보이게 ZTest Off/Write Off
        lineMat.SetInt("_ZTest", (int)CompareFunction.Always);
        lineMat.SetInt("_ZWrite", 0);
        lineMat.SetInt("_Cull", (int)CullMode.Off);
        lineMat.enableInstancing = true;
    }

    // ------- 카메라 훅(SRP/내장 겸용) -------
    void HookCamera(bool on)
    {
        if (GraphicsSettings.currentRenderPipeline != null)
        {
            if (on) RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
            else RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        }
        else
        {
            if (on) Camera.onPostRender += OnPostRenderCam;
            else Camera.onPostRender -= OnPostRenderCam;
        }
    }
    void OnEndCameraRendering(ScriptableRenderContext ctx, Camera cam)
    {
        if (!ShouldDrawOn(cam)) return;
        DrawAll(cam);
    }
    void OnPostRenderCam(Camera cam)
    {
        if (!ShouldDrawOn(cam)) return;
        DrawAll(cam);
    }
    bool ShouldDrawOn(Camera cam)
    {
        if (!enabledRendering || cam == null) return false;
        // Game 뷰와 Scene 뷰 둘 다 지원 (원하면 옵션화)
        return cam.cameraType == CameraType.Game || cam.cameraType == CameraType.SceneView;
    }

    // ------- 수집+그리기 -------
    readonly List<BoxComponent> temp = new();
    void DrawAll(Camera cam)
    {
        CollectBoxes(temp);

        GL.PushMatrix();
        lineMat.SetPass(0);
        GL.LoadProjectionMatrix(cam.projectionMatrix);
        GL.modelview = cam.worldToCameraMatrix;

        GL.Begin(GL.LINES);
        for (int i = 0; i < temp.Count; i++)
        {
            var b = temp[i];
            if (b == null || b.owner == null) continue;

            Color col = b.type switch
            {
                BoxType.Body => new Color(0.2f, 0.6f, 1f, 1f),
                BoxType.Hurt => new Color(0.2f, 1f, 0.2f, 1f),
                BoxType.Hit => new Color(1f, 0.2f, 0.2f, 1f),
                BoxType.Throw => new Color(1f, 0.9f, 0.2f, 1f),
                BoxType.GuardTrigger => new Color(0.2f, 1f, 1f, 1f),
                _ => Color.white
            };
            GL.Color(col);
            DrawRectLines(b);
        }
        GL.End();
        GL.PopMatrix();

        temp.Clear();
    }

    void CollectBoxes(List<BoxComponent> dst)
    {
        dst.Clear();

        // 1) 플레이 중: 충돌 우선 BoxManager 사용(스킬 히트박스 포함)
        if (Application.isPlaying && preferBoxManager && BoxManager.Instance != null)
        {
            var list = BoxManager.Instance.activeBoxes;
            for (int i = 0; i < list.Count; i++)
            {
                var b = list[i];
                if (b == null || b.owner == null) continue;
                if (!Filter(b)) continue;
                dst.Add(b);
            }
            // Body/Hurt 기본박스도 보여주고 싶다면 추가 수집
            if (drawBody || drawHurt)
            {
                var ents = BoxManager.Instance != null
    ? BoxManager.Instance.activeBoxes.Where(b => b && b.owner != null).Select(b => b.owner).Distinct().ToArray()
    : System.Array.Empty<PhysicsEntity>();
                foreach (var e in ents)
                {
                    if (e == null) continue;
                    if (drawBody && e.currentBodyBox != null) dst.Add(e.currentBodyBox);
                    if (drawHurt && e.currentHurtBoxes != null)
                        foreach (var hb in e.currentHurtBoxes) if (hb != null) dst.Add(hb);
                    if (drawHit && includeWhiffAsHit && e.currentWhiffBoxes != null)
                        foreach (var wb in e.currentWhiffBoxes) if (wb != null) dst.Add(wb);
                }
            }
            return;
        }

        // 2) 비플레이/프리팹/혹은 preferBoxManager=false
        if (currentPoseOnly)
        {
            var ents = BoxManager.Instance != null
    ? BoxManager.Instance.activeBoxes.Where(b => b && b.owner != null).Select(b => b.owner).Distinct().ToArray()
    : System.Array.Empty<PhysicsEntity>();
            foreach (var e in ents)
            {
                if (e == null || e.property == null) continue;
                if (drawBody && e.currentBodyBox != null) dst.Add(e.currentBodyBox);
                if (drawHurt && e.currentHurtBoxes != null)
                    foreach (var hb in e.currentHurtBoxes) if (hb != null) dst.Add(hb);
                if (drawHit && includeWhiffAsHit && e.currentWhiffBoxes != null)
                    foreach (var wb in e.currentWhiffBoxes) if (wb != null) dst.Add(wb);
            }
        }
        else
        {
            var all = (Application.isPlaying && BoxManager.Instance != null)
    ? BoxManager.Instance.activeBoxes.Where(b => b != null).ToArray()
    : System.Array.Empty<BoxComponent>();
            foreach (var b in all) if (b != null && b.owner != null && Filter(b)) dst.Add(b);
        }
    }

    bool Filter(BoxComponent b)
    {
        return (drawBody && b.type == BoxType.Body)
            || (drawHurt && b.type == BoxType.Hurt)
            || (drawHit && b.type == BoxType.Hit)
            || (drawThrow && b.type == BoxType.Throw)
            || (drawGuard && b.type == BoxType.GuardTrigger);
    }

    void DrawRectLines(BoxComponent b)
    {
        var r = b.GetAABB();
        float z = b.owner.transform.position.z;

        Vector3 bl = new(r.xMin, r.yMin, z);
        Vector3 tl = new(r.xMin, r.yMax, z);
        Vector3 tr = new(r.xMax, r.yMax, z);
        Vector3 br = new(r.xMax, r.yMin, z);

        Line(bl, tl); Line(tl, tr); Line(tr, br); Line(br, bl);
    }
    static void Line(in Vector3 a, in Vector3 b)
    {
        GL.Vertex(a);
        GL.Vertex(b);
    }
}
