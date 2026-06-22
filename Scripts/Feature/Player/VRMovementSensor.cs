using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using System.Collections;

public class VRMovementSensor : MonoBehaviour, IPlayerSensor
{
    [Header("UI Objects")]
    public GameObject eventUIPanel;
    public Text timerText;
    public Text warningText;

    [Space(10)]
    public Image headInnerCircle;
    public Image leftHandInnerCircle;
    public Image rightHandInnerCircle;

    [Header("Event Settings")]
    public float maxTension = 5f;
    public float gainMultiplier = 15f;
    public float decayRate = 1.0f;

    [Header("Sensor Deadzone")]
    public float movementDeadzone = 0.15f;

    [Header("Virtual Movement Settings")]
    public bool detectVirtualMovement = true;
    public float virtualGainMultiplier = 15f;

    [Tooltip("순수 이동 변위 계산을 위한 최상위 추적 타겟 지정 (회전 및 셰이크 오프셋 배제용)")]
    public Transform playerTrackingTarget;

    [Header("Colors")]
    public Color safeColor = Color.green;
    public Color dangerColor = Color.red;

    public bool IsMoving { get; private set; }
    public float TensionRatio { get; private set; }

    // 이벤트 가동 상태 프로퍼티
    public bool IsEventActive => isEventActive;

    private bool isEventActive = false;
    private Coroutine currentEventCoroutine = null;

    private float tensionHead, tensionLeft, tensionRight;
    private Vector3 lastPosition;
    private Quaternion lastRotation;

    void Start()
    {
        if (eventUIPanel != null) eventUIPanel.SetActive(false);

        // 타겟 미지정 시 메인 카메라 부모 계층을 기본 타겟으로 설정
        if (playerTrackingTarget == null && Camera.main != null)
        {
            if (Camera.main.transform.parent != null)
                playerTrackingTarget = Camera.main.transform.parent;
            else
                playerTrackingTarget = Camera.main.transform;
        }

        if (playerTrackingTarget != null)
        {
            lastPosition = playerTrackingTarget.position;
            lastRotation = playerTrackingTarget.rotation;
        }
    }

    void Update()
    {
        if (isEventActive)
        {
            AnalyzeMovement();
        }

        if (eventUIPanel != null && eventUIPanel.activeInHierarchy)
        {
            UpdateUIPositionAndRotation();
        }

        if (playerTrackingTarget != null)
        {
            lastPosition = playerTrackingTarget.position;
            lastRotation = playerTrackingTarget.rotation;
        }
    }

    private void UpdateUIPositionAndRotation()
    {
        if (eventUIPanel != null && Camera.main != null)
        {
            Transform camTransform = Camera.main.transform;
            Vector3 targetPosition = camTransform.position + camTransform.forward * 0.6f + camTransform.up * -0.1f;

            eventUIPanel.transform.position = Vector3.Lerp(eventUIPanel.transform.position, targetPosition, Time.deltaTime * 10f);
            eventUIPanel.transform.rotation = Quaternion.LookRotation(eventUIPanel.transform.position - camTransform.position);
        }
    }

    public void StartMovementEvent(float prepTime, float activeTime)
    {
        if (currentEventCoroutine != null)
        {
            StopCoroutine(currentEventCoroutine);
        }

        IsMoving = false;
        TensionRatio = 0f;
        tensionHead = tensionLeft = tensionRight = 0f;
        isEventActive = false;

        currentEventCoroutine = StartCoroutine(RunDonMoveEvent(prepTime, activeTime));
    }

    IEnumerator RunDonMoveEvent(float prepTime, float activeTime)
    {
        if (eventUIPanel != null)
        {
            if (Camera.main != null)
            {
                Transform camTransform = Camera.main.transform;
                eventUIPanel.transform.position = camTransform.position + camTransform.forward * 0.6f + camTransform.up * -0.1f;
                eventUIPanel.transform.rotation = Quaternion.LookRotation(eventUIPanel.transform.position - camTransform.position);
            }

            eventUIPanel.SetActive(true);
            if (headInnerCircle != null) headInnerCircle.rectTransform.localScale = Vector3.zero;
            if (leftHandInnerCircle != null) leftHandInnerCircle.rectTransform.localScale = Vector3.zero;
            if (rightHandInnerCircle != null) rightHandInnerCircle.rectTransform.localScale = Vector3.zero;
        }

        if (warningText != null) warningText.text = "좀비가 접근 중입니다!";

        float timer = prepTime;
        while (timer > 0)
        {
            if (timerText != null) timerText.text = timer.ToString("F1");
            timer -= Time.deltaTime;
            yield return null;
        }

        if (playerTrackingTarget != null)
        {
            lastPosition = playerTrackingTarget.position;
            lastRotation = playerTrackingTarget.rotation;
        }

        // 모션 감지 구간 중 우측 컨트롤러 조작 제한
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.SetRightHandInteractions(false);
        }

        tensionHead = tensionLeft = tensionRight = 0f;
        IsMoving = false;
        isEventActive = true;

        if (warningText != null) warningText.text = "움직이지 마세요!";

        float remainingTime = activeTime;

        while (remainingTime > 0 && !IsMoving)
        {
            remainingTime -= Time.deltaTime;
            if (timerText != null) timerText.text = $"남은 시간: {remainingTime:F1}s";

            if (tensionHead >= maxTension || tensionLeft >= maxTension || tensionRight >= maxTension)
            {
                IsMoving = true;
            }
            yield return null;
        }

        isEventActive = false;

        if (IsMoving)
        {
            if (warningText != null) warningText.text = "<color=red>움직임 감지!</color>";
        }
        else
        {
            if (warningText != null) warningText.text = "<color=green>상황 종료.</color>";
        }

        // 모션 감지 종료 후 조작 제한 해제
        if (player != null)
        {
            player.SetRightHandInteractions(true);
        }

        yield return new WaitForSeconds(3f);

        if (eventUIPanel != null) eventUIPanel.SetActive(false);
        currentEventCoroutine = null;
    }

    private void AnalyzeMovement()
    {
        float virtualSpeed = 0f;

        // 가상 이동(조이스틱 등) 감지 로직
        if (detectVirtualMovement && playerTrackingTarget != null)
        {
            // 월드 좌표 변화량 기반의 순수 이동 속도 산출
            float distanceMoved = (playerTrackingTarget.position - lastPosition).magnitude;
            float positionSpeed = distanceMoved / Time.deltaTime;

            // 카메라 회전 효과에 의한 데드존 오인 판정 방지
            if (positionSpeed > movementDeadzone)
            {
                virtualSpeed = positionSpeed;
            }
        }

        float virtualTensionAdd = virtualSpeed * virtualGainMultiplier;

        // 하드웨어 디바이스 속도 데이터 취합 및 가상 이동량 병합
        float hVel = GetDeviceSpeed(XRNode.CenterEye) + virtualTensionAdd;
        float lVel = GetDeviceSpeed(XRNode.LeftHand) + virtualTensionAdd;
        float rVel = GetDeviceSpeed(XRNode.RightHand) + virtualTensionAdd;

        tensionHead = CalculateTension(tensionHead, hVel);
        tensionLeft = CalculateTension(tensionLeft, lVel);
        tensionRight = CalculateTension(tensionRight, rVel);

        float maxCurrentTension = Mathf.Max(tensionHead, tensionLeft, tensionRight);
        TensionRatio = maxCurrentTension / maxTension;

        UpdateIndicator(headInnerCircle, tensionHead);
        UpdateIndicator(leftHandInnerCircle, tensionLeft);
        UpdateIndicator(rightHandInnerCircle, tensionRight);
    }

    private float GetDeviceSpeed(XRNode node)
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(node);
        Vector3 velocity = Vector3.zero;
        Vector3 angularVelocity = Vector3.zero;

        device.TryGetFeatureValue(CommonUsages.deviceVelocity, out velocity);
        device.TryGetFeatureValue(CommonUsages.deviceAngularVelocity, out angularVelocity);

        float linearSpeed = velocity.magnitude;
        float angularSpeed = angularVelocity.magnitude * 0.1f;

        if (linearSpeed < movementDeadzone) linearSpeed = 0f;
        if (angularSpeed < movementDeadzone) angularSpeed = 0f;

        return linearSpeed + angularSpeed;
    }

    private float CalculateTension(float currentVal, float speed)
    {
        if (speed > 0f)
        {
            currentVal += speed * gainMultiplier * Time.deltaTime;
        }
        else
        {
            currentVal -= decayRate * Time.deltaTime;
        }
        return Mathf.Clamp(currentVal, 0, maxTension);
    }

    private void UpdateIndicator(Image img, float tensionVal)
    {
        if (img == null) return;
        float ratio = tensionVal / maxTension;
        img.rectTransform.localScale = new Vector3(ratio, ratio, 1f);
        img.color = Color.Lerp(safeColor, dangerColor, ratio);
    }
}