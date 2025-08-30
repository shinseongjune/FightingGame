// SelectSceneInputRouter.cs
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(UGUISelectSceneView))]
public class SelectSceneInputRouter : MonoBehaviour
{
    [Header("Repeat (optional)")]
    [SerializeField] bool useRepeat = true;
    [SerializeField] float initialRepeatDelay = 0.35f;
    [SerializeField] float repeatInterval = 0.10f;
    [SerializeField] float deadzone = 0.5f;

    ISelectSceneView _view;

    // P1 / P2 액션 맵
    InputActionAsset _asset;
    InputActionMap _p1, _p2;
    InputAction _p1Move, _p1Submit, _p1Cancel, _p1Random;
    InputAction _p2Move, _p2Submit, _p2Cancel, _p2Random;

    // 반복 네비용 코루틴 핸들
    Coroutine _p1RepeatCo, _p2RepeatCo;

    UGUISelectSceneView _emitter;

    void Awake()
    {
        _emitter = GetComponent<UGUISelectSceneView>();
        _view = _emitter;
        BuildActions();
        BindEvents();
        _asset.Enable();
    }

    void OnDestroy()
    {
        UnbindEvents();
        if (_asset != null) _asset.Disable();
        Destroy(_asset);
    }

    // ----- 액션 구성 -----
    void BuildActions()
    {
        _asset = ScriptableObject.CreateInstance<InputActionAsset>();

        _p1 = new InputActionMap("P1");
        _p2 = new InputActionMap("P2");

        // 공통 액션들
        _p1Move = _p1.AddAction("Move", InputActionType.Value);
        _p1Move.expectedControlType = "Vector2";
        _p1Submit = _p1.AddAction("Submit", InputActionType.Button);
        _p1Cancel = _p1.AddAction("Cancel", InputActionType.Button);
        _p1Random = _p1.AddAction("Random", InputActionType.Button);

        _p2Move = _p2.AddAction("Move", InputActionType.Value);
        _p2Move.expectedControlType = "Vector2";
        _p2Submit = _p2.AddAction("Submit", InputActionType.Button);
        _p2Cancel = _p2.AddAction("Cancel", InputActionType.Button);
        _p2Random = _p2.AddAction("Random", InputActionType.Button);

        // ----- 바인딩 (Keyboard) -----
        // P1: 방향키 / Enter / Backspace / RightShift
        _p1Move.AddCompositeBinding("2DVector")
            .With("up", "<Keyboard>/upArrow")
            .With("down", "<Keyboard>/downArrow")
            .With("left", "<Keyboard>/leftArrow")
            .With("right", "<Keyboard>/rightArrow");
        _p1Submit.AddBinding("<Keyboard>/enter");
        _p1Cancel.AddBinding("<Keyboard>/backspace");
        _p1Random.AddBinding("<Keyboard>/rightShift");

        // P2: WASD / Space / LeftCtrl / LeftShift
        _p2Move.AddCompositeBinding("2DVector")
            .With("up", "<Keyboard>/w")
            .With("down", "<Keyboard>/s")
            .With("left", "<Keyboard>/a")
            .With("right", "<Keyboard>/d");
        _p2Submit.AddBinding("<Keyboard>/space");
        _p2Cancel.AddBinding("<Keyboard>/leftCtrl");
        _p2Random.AddBinding("<Keyboard>/leftShift");

        // ----- 바인딩 (Gamepad) -----
        // P1
        _p1Move.AddBinding("<Gamepad>/leftStick");
        _p1Move.AddBinding("<Gamepad>/dpad");
        _p1Submit.AddBinding("<Gamepad>/buttonSouth"); // A/✕
        _p1Cancel.AddBinding("<Gamepad>/buttonEast");  // B/○
        _p1Random.AddBinding("<Gamepad>/buttonNorth"); // Y/△

        // P2 (다중 패드 환경이면 디바이스를 분리해서 PlayerInput으로 라우팅 권장)
        _p2Move.AddBinding("<Gamepad>/leftStick");
        _p2Move.AddBinding("<Gamepad>/dpad");
        _p2Submit.AddBinding("<Gamepad>/buttonSouth");
        _p2Cancel.AddBinding("<Gamepad>/buttonEast");
        _p2Random.AddBinding("<Gamepad>/buttonNorth");

        _asset.AddActionMap(_p1);
        _asset.AddActionMap(_p2);
    }

    void BindEvents()
    {
        // P1
        _p1Move.performed += ctx => HandleMove(ctx, PlayerId.P1);
        _p1Move.canceled += ctx => StopRepeat(PlayerId.P1);
        _p1Submit.performed += _ => _emitter.EmitSubmit(PlayerId.P1);
        _p1Cancel.performed += _ => _emitter.EmitCancel(PlayerId.P1);
        _p1Random.performed += _ => _emitter.EmitRandom(PlayerId.P1);

        // P2
        _p2Move.performed += ctx => HandleMove(ctx, PlayerId.P2);
        _p2Move.canceled += ctx => StopRepeat(PlayerId.P2);
        _p2Submit.performed += _ => _emitter.EmitSubmit(PlayerId.P1);
        _p2Cancel.performed += _ => _emitter.EmitCancel(PlayerId.P1);
        _p2Random.performed += _ => _emitter.EmitRandom(PlayerId.P1);
    }

    void UnbindEvents()
    {
        _p1Move.performed -= ctx => HandleMove(ctx, PlayerId.P1);
        _p1Move.canceled -= ctx => StopRepeat(PlayerId.P1);
        _p1Submit.performed -= _ => _emitter.EmitSubmit(PlayerId.P1);
        _p1Cancel.performed -= _ => _emitter.EmitCancel(PlayerId.P1);
        _p1Random.performed -= _ => _emitter.EmitRandom(PlayerId.P1);

        _p2Move.performed -= ctx => HandleMove(ctx, PlayerId.P2);
        _p2Move.canceled -= ctx => StopRepeat(PlayerId.P2);
        _p2Submit.performed -= _ => _emitter.EmitSubmit(PlayerId.P1);
        _p2Cancel.performed -= _ => _emitter.EmitCancel(PlayerId.P1);
        _p2Random.performed -= _ => _emitter.EmitRandom(PlayerId.P1);
    }

    // ----- 이동 처리 -----
    void HandleMove(InputAction.CallbackContext ctx, PlayerId pid)
    {
        Vector2 v = ctx.ReadValue<Vector2>();
        // 카디널화
        Vector2Int dir = ToCardinal(v, deadzone);
        if (dir == Vector2Int.zero) return;

        // 1회 발화
        _emitter.EmitNavigate(dir, pid);

        // 반복 네비
        if (useRepeat)
        {
            StopRepeat(pid);
            if (pid == PlayerId.P1) _p1RepeatCo = StartCoroutine(Repeat(dir, pid));
            else _p2RepeatCo = StartCoroutine(Repeat(dir, pid));
        }
    }

    IEnumerator Repeat(Vector2Int dir, PlayerId pid)
    {
        yield return new WaitForSeconds(initialRepeatDelay);
        while (true)
        {
            _emitter.EmitNavigate(dir, pid);
            yield return new WaitForSeconds(repeatInterval);
        }
    }

    void StopRepeat(PlayerId pid)
    {
        if (pid == PlayerId.P1 && _p1RepeatCo != null) { StopCoroutine(_p1RepeatCo); _p1RepeatCo = null; }
        if (pid == PlayerId.P2 && _p2RepeatCo != null) { StopCoroutine(_p2RepeatCo); _p2RepeatCo = null; }
    }

    static Vector2Int ToCardinal(Vector2 v, float dz)
    {
        if (Mathf.Abs(v.x) >= Mathf.Abs(v.y))
        {
            if (v.x > dz) return Vector2Int.right;
            if (v.x < -dz) return Vector2Int.left;
        }
        else
        {
            if (v.y > dz) return Vector2Int.up;
            if (v.y < -dz) return Vector2Int.down;
        }
        return Vector2Int.zero;
    }
}
