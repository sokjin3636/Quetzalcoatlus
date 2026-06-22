using UnityEngine;
using UnityEngine.UI;

public class SettingsController : MonoBehaviour
{
    [Header("Sound")]
    public Slider soundSlider;

    [Header("World Brightness")]
    public Slider brightnessSlider;

    [Header("Brightness Range")]
    public float minAmbientIntensity = 0f;
    public float maxAmbientIntensity = 1.0f;

    private void Start()
    {
        // 로컬 저장소(PlayerPrefs)에서 환경 설정값 로드 및 초기화
        float savedVolume = PlayerPrefs.GetFloat("Volume", 0.7f);
        float savedBrightness = PlayerPrefs.GetFloat("Brightness", 0.1f);

        soundSlider.value = savedVolume;
        brightnessSlider.value = savedBrightness;

        SetVolume(savedVolume);
        SetBrightness(savedBrightness);

        // 슬라이더 이벤트 리스너 등록
        soundSlider.onValueChanged.AddListener(SetVolume);
        brightnessSlider.onValueChanged.AddListener(SetBrightness);
    }

    // 글로벌 오디오 볼륨 설정 및 저장
    public void SetVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("Volume", value);
    }

    // 씬 내 환경광(Ambient) 강도 설정 및 저장
    public void SetBrightness(float value)
    {
        float intensity = Mathf.Lerp(minAmbientIntensity, maxAmbientIntensity, value);
        RenderSettings.ambientIntensity = intensity;
        PlayerPrefs.SetFloat("Brightness", value);
    }
}