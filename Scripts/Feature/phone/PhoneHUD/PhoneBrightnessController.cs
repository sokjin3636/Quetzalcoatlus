using UnityEngine;
using UnityEngine.UI;

public class PhoneBrightnessController : MonoBehaviour
{
    [Header("UI")]
    public Image brightnessOverlay;

    [Header("Glow Light")]
    public Light screenGlowLight;

    [Header("Brightness")]
    [Range(0f, 1f)]
    public float brightness = 1f;

    [Header("Glow")]
    public float maxGlowIntensity = 0.5f;

    void Start()
    {
        ApplyBrightness();
    }

    public void SetBrightness(float value)
    {
        brightness = Mathf.Clamp01(value);
        ApplyBrightness();
    }

    public void IncreaseBrightness(float amount = 0.1f)
    {
        brightness = Mathf.Clamp01(brightness + amount);
        ApplyBrightness();
    }

    public void DecreaseBrightness(float amount = 0.1f)
    {
        brightness = Mathf.Clamp01(brightness - amount);
        ApplyBrightness();
    }

    public void SetScreenActive(bool active)
    {
        if (brightnessOverlay != null)
            brightnessOverlay.gameObject.SetActive(active);

        if (screenGlowLight != null)
            screenGlowLight.enabled = active && brightness > 0.01f;
    }

    private void ApplyBrightness()
    {
        // 1. UI 화면 밝기 적용 (알파값 반전)
        if (brightnessOverlay != null)
        {
            Color c = brightnessOverlay.color;
            c.a = 1f - brightness;
            brightnessOverlay.color = c;
        }

        // 2. 화면 주변 광원(Glow Light) 강도 적용
        if (screenGlowLight != null)
        {
            screenGlowLight.intensity = brightness * maxGlowIntensity;
            screenGlowLight.enabled = brightness > 0.01f;
        }
    }

    // 시스템 강제 종료/잠금 이벤트 시 붉은색 오버레이 처리
    public void SetPowerLockOverlay(bool locked)
    {
        if (brightnessOverlay == null)
            return;

        brightnessOverlay.color = locked
            ? new Color(1f, 0f, 0f, 1f)
            : new Color(0f, 0f, 0f, 0f);
    }
}