using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

public class SprintMoveProvider : ContinuousMoveProvider
{
    [Header("Move Speed")]
    [SerializeField] private float walkSpeed = 1f;
    [SerializeField] private float runSpeed = 4f;

    [Header("Sprint Button")]
    [SerializeField] private InputActionProperty sprintButton;

    [Header("--- 달리기 오디오 설정 ---")]
    [SerializeField] private AudioSource runFootstepAudio;

    [Header("--- 스태미너 시스템 설정 ---")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDrainRate = 20f;
    [SerializeField] private float staminaRegenRate = 15f;
    private float currentStamina;
    private bool isExhausted = false;

    [Header("--- 긴장도 시스템 연동 및 스태미너 소모율 설정 ---")]
    [SerializeField] private InGameTensionSystem tensionSystem;

    [Tooltip("1단계 (Calm): 기본 소모율")]
    [SerializeField] private float calmDrainMultiplier = 1.0f;

    [Tooltip("2단계 (Tension): 가속 소모율")]
    [SerializeField] private float tensionDrainMultiplier = 1.4f;

    [Tooltip("3단계 (Panic): 최대 소모율")]
    [SerializeField] private float panicDrainMultiplier = 2.0f;

    [Header("--- 스태미너 UI 컴포넌트 연결 ---")]
    [SerializeField] private Image staminaFillImage;
    [SerializeField] private Image staminaOutlineImage;

    [Header("--- 상태별 채우기(Fill) 색상 설정 ---")]
    [SerializeField] private Color walkFillColor = new Color(0.2f, 0.5f, 0.2f, 0.5f);
    [SerializeField] private Color runFillColor = new Color(0.4f, 1.0f, 0.4f, 1.0f);

    [Header("--- 상태별 테두리(Outline) 색상 설정 ---")]
    [SerializeField] private Color walkOutlineColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
    [SerializeField] private Color runOutlineColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

    private bool isSprintToggled = false;

    // 외부 참조 프로퍼티
    public bool IsSprintToggled => isSprintToggled;

    protected void OnEnable()
    {
        currentStamina = maxStamina;
    }

    protected new void OnDisable()
    {
        base.OnDisable();

        if (runFootstepAudio != null && runFootstepAudio.isPlaying)
        {
            runFootstepAudio.Stop();
        }
    }

    protected override void Update()
    {
        // 입력 벡터 크기를 통한 이동 여부 확인
        Vector2 moveInput = leftHandMoveInput.ReadValue() + rightHandMoveInput.ReadValue();
        bool isMoving = moveInput.magnitude > 0.1f;

        // 스프린트 토글 입력 처리
        if (sprintButton.action != null && sprintButton.action.WasPressedThisFrame())
        {
            if (!isExhausted)
            {
                isSprintToggled = !isSprintToggled;
                Debug.Log($"[Sprint] 달리기 모드 토글 변경: {isSprintToggled}");
            }
        }

        // 스태미너 증감 로직
        if (isSprintToggled && isMoving)
        {
            // 텐션 기반 스태미너 소모 배율 계산
            float currentMultiplier = GetCurrentTensionMultiplier();

            currentStamina -= (staminaDrainRate * currentMultiplier) * Time.deltaTime;

            if (currentStamina <= 0f)
            {
                currentStamina = 0f;
                isSprintToggled = false;
                isExhausted = true;
                Debug.Log("[Sprint] 스태미너 고갈! 강제로 걷기 모드로 강등됩니다.");
            }
        }
        else
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);

            if (isExhausted && currentStamina >= (maxStamina * 0.3f))
            {
                isExhausted = false;
                Debug.Log("[Sprint] 스태미너 충전 완료! 다시 달릴 수 있습니다.");
            }
        }

        // 이동 속도 및 발소리 오디오 제어
        if (isSprintToggled)
        {
            moveSpeed = runSpeed;

            if (isMoving)
            {
                if (runFootstepAudio != null && !runFootstepAudio.isPlaying) runFootstepAudio.Play();
            }
            else
            {
                if (runFootstepAudio != null && runFootstepAudio.isPlaying) runFootstepAudio.Stop();
            }
        }
        else
        {
            moveSpeed = walkSpeed;
            if (runFootstepAudio != null && runFootstepAudio.isPlaying) runFootstepAudio.Stop();
        }

        // 스태미너 UI 시각 효과 갱신
        UpdateStaminaUI();

        base.Update();
    }

    // 현재 긴장도 수치 기반 스태미너 소모 배율 산출
    private float GetCurrentTensionMultiplier()
    {
        if (tensionSystem == null) return 1.0f;

        float tension = tensionSystem.currentTension;

        // 긴장도 구간별 배율 적용
        if (tension >= 70f)
        {
            return panicDrainMultiplier;
        }
        else if (tension >= 35f)
        {
            return tensionDrainMultiplier;
        }
        else
        {
            return calmDrainMultiplier;
        }
    }

    private void UpdateStaminaUI()
    {
        // 스태미너 게이지 비율 및 색상 적용
        if (staminaFillImage != null)
        {
            staminaFillImage.fillAmount = currentStamina / maxStamina;
            staminaFillImage.color = isSprintToggled ? runFillColor : walkFillColor;
        }

        // 스프린트 및 탈진 상태 테두리(Outline) 시각 효과 제어
        if (staminaOutlineImage != null)
        {
            if (isExhausted)
            {
                // 탈진 상태 시 붉은색 경고 표시
                staminaOutlineImage.color = new Color(1f, 0f, 0f, 0.5f);
            }
            else
            {
                staminaOutlineImage.color = isSprintToggled ? runOutlineColor : walkOutlineColor;
            }
        }
    }
}