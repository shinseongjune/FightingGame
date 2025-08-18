using UnityEngine;
using UnityEngine.InputSystem;

public class RecognizerTraceInput : MonoBehaviour
{
    InputAction freeze, stepL, stepR;

    void OnEnable()
    {
        freeze = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/f1");
        freeze.AddBinding("<Gamepad>/start");
        freeze.performed += _ => GamePause.Toggle();               // ★ 전역 일시정지
        freeze.Enable();

        stepL = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/leftArrow");
        stepL.AddBinding("<Gamepad>/dpad/left");
        stepL.performed += _ => RecognizerTrace.Step(-1);
        stepL.Enable();

        stepR = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/rightArrow");
        stepR.AddBinding("<Gamepad>/dpad/right");
        stepR.performed += _ => RecognizerTrace.Step(+1);
        stepR.Enable();
    }
    void OnDisable()
    {
        freeze?.Disable(); stepL?.Disable(); stepR?.Disable();
    }
}
