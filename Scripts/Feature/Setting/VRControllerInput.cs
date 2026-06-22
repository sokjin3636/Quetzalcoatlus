using UnityEngine;
using UnityEngine.InputSystem;

public class VRControllerInput : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionReference pauseAction;
    public InputActionReference powerAction;
    public InputActionReference flashAction;

    [Header("References")]
    public PauseController pauseController;
    public PhonePowerController phonePowerController;

    private void OnEnable()
    {
        if (pauseAction != null)
            pauseAction.action.Enable();

        if (powerAction != null)
            powerAction.action.Enable();

        if (flashAction != null)
            flashAction.action.Enable();
    }

    private void OnDisable()
    {
        if (pauseAction != null)
            pauseAction.action.Disable();

        if (powerAction != null)
            powerAction.action.Disable();

        if (flashAction != null)
            flashAction.action.Disable();
    }

    private void Update()
    {
        // 일시정지 트리거 입력 감지
        if (pauseAction != null &&
            pauseAction.action.WasPressedThisFrame())
        {
            if (pauseController != null)
                pauseController.TogglePause();
        }

        // 스마트폰 전원 토글 입력 감지
        if (powerAction != null &&
            powerAction.action.WasPressedThisFrame())
        {
            if (phonePowerController != null)
                phonePowerController.TogglePhonePower();
        }

        // 플래시라이트 토글 입력 감지
        if (flashAction != null &&
            flashAction.action.WasPressedThisFrame())
        {
            if (phonePowerController != null)
                phonePowerController.ToggleFlash();
        }
    }
}