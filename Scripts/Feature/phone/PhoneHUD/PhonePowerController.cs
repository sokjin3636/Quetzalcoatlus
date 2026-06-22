using System.Collections;
using UnityEngine;

public class PhonePowerController : MonoBehaviour
{
    [Header("Phone UI")]
    public Canvas phoneCanvas;
    public Canvas phoneCanvas1;
    public Canvas phoneCanvas2;
    public GameObject phoneScreenObject;

    [Header("Power Lock UI")]
    public Canvas powerLockCanvas;

    [Header("Flash")]
    public Light flashLight;
    public bool flashStartsOn = false;

    [Header("Power")]
    public bool phoneStartsOn = true;
    public float powerOnDelay = 1.5f;

    [Header("Battery")]
    [Range(0f, 100f)]
    public float batteryPercent = 100f;

    public float fullBatterySeconds = 100f;
    public float lowBatteryWarningPercent = 15f;

    [Header("Forced Shutdown")]
    public float defaultPowerLockSeconds = 3f;

    [Header("Brightness")]
    public PhoneBrightnessController brightnessController;

    [Header("Navigation")]
    public PathController pathController;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip powerOnClip;
    public AudioClip lowBatteryClip;
    public AudioClip WarningClip;

    private bool isPhoneOn;
    private bool isFlashOn;
    private bool isBooting;
    private bool powerLocked;
    private bool lowBatteryLogged = false;

    private Coroutine powerOnCoroutine;
    private Coroutine powerLockCoroutine;

    public bool IsPhoneOn => isPhoneOn;
    public bool IsFlashOn => isFlashOn;
    public bool IsBooting => isBooting;
    public bool IsPowerLocked => powerLocked;
    public float BatteryPercent => batteryPercent;

    void Start()
    {
        isPhoneOn = phoneStartsOn && batteryPercent > 0f;
        isFlashOn = flashStartsOn && isPhoneOn;
        isBooting = false;
        powerLocked = false;

        ApplyPhoneState();
        ApplyFlashState();
    }

    void Update()
    {
        if (!isPhoneOn)
            return;

        DrainBattery();

        if (batteryPercent <= 0f)
        {
            batteryPercent = 0f;
            TurnPhoneOff();

            //Debug.Log("폰 배터리가 0%가 되어 전원이 꺼졌습니다.");
        }
    }

    // 시간 경과에 따른 배터리 소모 로직
    private void DrainBattery()
    {
        if (fullBatterySeconds <= 0f)
            return;

        float drainPerSecond = 100f / fullBatterySeconds;
        batteryPercent -= drainPerSecond * Time.deltaTime;
        batteryPercent = Mathf.Clamp(batteryPercent, 0f, 100f);

        if (!lowBatteryLogged && batteryPercent <= lowBatteryWarningPercent)
        {
            lowBatteryLogged = true;
            //Debug.Log($"폰 배터리 부족: {batteryPercent:F1}%");

            if (audioSource != null && lowBatteryClip != null)
                audioSource.PlayOneShot(lowBatteryClip);
        }
    }

    public void TogglePhonePower()
    {
        if (isBooting)
            return;

        if (isPhoneOn)
            TurnPhoneOff();
        else
            TurnPhoneOn();
    }

    public void ToggleFlash()
    {
        if (!isPhoneOn || isBooting || powerLocked)
            return;

        isFlashOn = !isFlashOn;
        ApplyFlashState();
    }

    public void TurnPhoneOn()
    {
        if (powerLocked)
        {
            //Debug.Log("현재는 이벤트로 인해 폰을 켤 수 없습니다.");
            return;
        }

        if (isBooting || isPhoneOn)
            return;

        if (batteryPercent <= 0f)
        {
            isPhoneOn = false;
            isFlashOn = false;
            isBooting = false;

            ApplyPhoneState();
            ApplyFlashState();

            //Debug.Log("배터리가 0%라서 폰을 켤 수 없습니다.");
            return;
        }

        if (powerOnCoroutine != null)
            StopCoroutine(powerOnCoroutine);

        powerOnCoroutine = StartCoroutine(PowerOnRoutine());
    }

    private IEnumerator PowerOnRoutine()
    {
        isBooting = true;

        if (audioSource != null && powerOnClip != null)
            audioSource.PlayOneShot(powerOnClip);

        //Debug.Log("폰 부팅 시작");

        yield return new WaitForSeconds(powerOnDelay);

        if (batteryPercent <= 0f || powerLocked)
        {
            isBooting = false;
            TurnPhoneOff();
            yield break;
        }

        isPhoneOn = true;
        isBooting = false;
        powerOnCoroutine = null;

        ApplyPhoneState();
        ApplyFlashState();

        //Debug.Log("폰 전원이 켜졌습니다.");
    }

    public void TurnPhoneOff()
    {
        if (powerOnCoroutine != null)
        {
            StopCoroutine(powerOnCoroutine);
            powerOnCoroutine = null;
        }

        isPhoneOn = false;
        isFlashOn = false;
        isBooting = false;

        ApplyPhoneState();
        ApplyFlashState();
    }

    // 외부 이벤트에 의한 강제 전원 차단 (기본 시간 적용)
    public void ForcePowerOffForSeconds()
    {
        ForcePowerOffForSeconds(defaultPowerLockSeconds);
    }

    // 외부 이벤트에 의한 강제 전원 차단 (지정 시간 적용)
    public void ForcePowerOffForSeconds(float seconds)
    {
        TurnPhoneOff();

        if (powerLockCoroutine != null)
            StopCoroutine(powerLockCoroutine);

        powerLockCoroutine = StartCoroutine(PowerLockRoutine(seconds));
    }

    private IEnumerator PowerLockRoutine(float seconds)
    {
        powerLocked = true;
        ApplyPhoneState();

        if (audioSource != null && WarningClip != null)
            audioSource.PlayOneShot(WarningClip);

        //Debug.Log($"이벤트로 폰 전원 차단: {seconds:F1}초 동안 켜기 불가");

        yield return new WaitForSeconds(seconds);

        powerLocked = false;
        powerLockCoroutine = null;

        ApplyPhoneState();

        //Debug.Log("폰 전원 차단 해제");
    }

    private void ApplyPhoneState()
    {
        if (phoneCanvas != null)
            phoneCanvas.enabled = isPhoneOn;

        if (phoneCanvas1 != null)
            phoneCanvas1.enabled = isPhoneOn;

        if (phoneCanvas2 != null)
            phoneCanvas2.enabled = isPhoneOn;

        if (phoneScreenObject != null)
            phoneScreenObject.SetActive(isPhoneOn);

        if (powerLockCanvas != null)
            powerLockCanvas.enabled = powerLocked;

        if (brightnessController != null)
            brightnessController.SetScreenActive(isPhoneOn);

        if (pathController != null)
            pathController.SetNavigationActive(isPhoneOn);
    }

    private void ApplyFlashState()
    {
        if (flashLight != null)
            flashLight.enabled = isPhoneOn && isFlashOn && !powerLocked;
    }

    public void RechargeBattery()
    {
        batteryPercent = 100f;
        lowBatteryLogged = false;

        //Debug.Log("폰 배터리 충전 완료: 100%");
    }
}