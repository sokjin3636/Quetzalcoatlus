using UnityEngine;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort;

public class TensionEffectManager : MonoBehaviour, ITunnelingVignetteProvider
{
    public InGameTensionSystem tensionSystem;
    public TunnelingVignetteController vignetteController;

    [Header("--- 비네트 범위 설정 ---")]
    [Range(0f, 1f)] public float minAperture = 0.2f;
    [Range(0f, 1f)] public float maxAperture = 1.0f;
    [Range(0f, 1f)] public float feathering = 0.2f;

    [Header("--- 터널링 비주얼 설정 ---")]
    [Tooltip("터널링 적용 메인 색상 및 투명도(Alpha)")]
    public Color vignetteColor = Color.black;
    [Tooltip("터널링 외곽 블렌딩 색상")]
    public Color vignetteColorBlend = Color.black;

    [Header("--- 사운드 설정 ---")]
    public AudioSource heartbeatSource;

    [Header("--- 카메라 셰이크(Wobble) 설정 ---")]
    [Tooltip("VR HMD 트래킹 간섭 방지를 위해 Camera Offset(상위 Transform) 할당 필요")]
    public Transform xrCameraOffsetTransform;

    [Tooltip("최고 긴장도 기준 회전 한계 각도")]
    public float maxRotationAngle = 6.0f;

    [Tooltip("카메라 셰이크 주파수(속도)")]
    public float shakeSpeed = 22f;

    private Quaternion originalOffsetRotation;

    private VignetteParameters m_Params = new VignetteParameters();
    public VignetteParameters vignetteParameters => m_Params;

    void Start()
    {
        if (vignetteController != null)
        {
            m_Params.featheringEffect = feathering;
            m_Params.vignetteColor = vignetteColor;
            m_Params.vignetteColorBlend = vignetteColorBlend;
            vignetteController.BeginTunnelingVignette(this);
            m_Params.apertureSize = 1.0f;
        }

        if (xrCameraOffsetTransform != null)
        {
            originalOffsetRotation = xrCameraOffsetTransform.localRotation;
        }
    }

    void Update()
    {
        if (tensionSystem == null || vignetteController == null) return;

        float tension = tensionSystem.currentTension;
        float t = tension / 100f;

        // 1. 비네트 강도 및 설정 업데이트
        m_Params.apertureSize = Mathf.Lerp(maxAperture, minAperture, t);
        m_Params.featheringEffect = feathering;
        m_Params.vignetteColor = vignetteColor;
        m_Params.vignetteColorBlend = vignetteColorBlend;

        // 2. 심박음 사운드 효과 제어
        ApplyAudio(t);

        // 3. 카메라 셰이크 울렁거림 적용
        ApplyScreenWobble(t);
    }

    void ApplyAudio(float t)
    {
        if (heartbeatSource == null) return;

        if (t > 0.33f)
        {
            if (!heartbeatSource.isPlaying) heartbeatSource.Play();
            float normalizedT = (t - 0.33f) / 0.67f;
            heartbeatSource.volume = Mathf.Lerp(0.1f, 0.8f, normalizedT);
            heartbeatSource.pitch = Mathf.Lerp(1.0f, 1.5f, normalizedT);
        }
        else
        {
            heartbeatSource.volume = 0;
        }
    }

    // Camera Offset에 펄린 노이즈 기반 회전 변위를 적용하여 카메라 셰이크 연출 수행
    void ApplyScreenWobble(float t)
    {
        if (xrCameraOffsetTransform == null) return;

        if (t <= 0.33f)
        {
            xrCameraOffsetTransform.localRotation = originalOffsetRotation;
            return;
        }

        // 33% ~ 100% 구간 정규화를 통한 셰이크 강도 연산
        float intensityFactor = (t - 0.33f) / 0.67f;
        float currentIntensity = Mathf.Lerp(0f, maxRotationAngle, intensityFactor);

        float shakeX = (Mathf.PerlinNoise(Time.time * shakeSpeed, 0f) * 2f - 1f) * currentIntensity;
        float shakeY = (Mathf.PerlinNoise(0f, Time.time * shakeSpeed) * 2f - 1f) * currentIntensity;
        float shakeZ = (Mathf.PerlinNoise(Time.time * shakeSpeed, Time.time * shakeSpeed) * 2f - 1f) * currentIntensity;

        xrCameraOffsetTransform.localRotation = originalOffsetRotation * Quaternion.Euler(shakeX, shakeY, shakeZ);
    }

    void OnDisable()
    {
        if (vignetteController != null)
        {
            vignetteController.EndTunnelingVignette(this);
        }

        // 비활성화 시 오프셋 회전값 초기화
        if (xrCameraOffsetTransform != null)
        {
            xrCameraOffsetTransform.localRotation = originalOffsetRotation;
        }
    }
}