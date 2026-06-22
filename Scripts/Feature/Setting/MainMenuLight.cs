using System.Collections;
using UnityEngine;

public class MainMenuLight : MonoBehaviour
{
    [Header("Light")]
    public Light targetLight;

    [Header("Mesh Renderer (깜빡일 오브젝트)")]
    public Renderer targetRenderer;

    [Header("Intensity")]
    public float startIntensity = 1f;
    public float dimIntensity = 0.1f;
    public float flickerIntensity = 0.4f;

    [Header("Timing")]
    public float dimDuration = 2f;
    public float flickerInterval = 0.08f;
    public float restoreDuration = 1f;
    public float waitBetweenCycles = 1f;

    [Header("Flicker")]
    public int flickerCount = 5;

    private Material mat;

    private void Start()
    {
        if (targetRenderer != null)
            mat = targetRenderer.material;

        StartCoroutine(FlickerLoop());
    }

    private IEnumerator FlickerLoop()
    {
        while (true)
        {
            // 1. 조명 강도 점진적 감소 페이즈
            yield return StartCoroutine(
                LerpIntensity(startIntensity, dimIntensity, dimDuration)
            );

            SetVisual(flickerIntensity);

            // 2. 일시적 점멸(Flicker) 효과 연산 수행
            for (int i = 0; i < flickerCount; i++)
            {
                SetVisual(0f);
                yield return new WaitForSeconds(flickerInterval);

                SetVisual(flickerIntensity);
                yield return new WaitForSeconds(flickerInterval);
            }

            // 3. 원상태로 조명 강도 점진적 복구 페이즈
            yield return StartCoroutine(
                LerpIntensity(flickerIntensity, startIntensity, restoreDuration)
            );

            yield return new WaitForSeconds(waitBetweenCycles);
        }
    }

    // 시간 경과에 따른 조명 강도 선형 보간(Lerp) 처리
    private IEnumerator LerpIntensity(float from, float to, float duration)
    {
        float time = 0f;

        while (time < duration)
        {
            float t = time / duration;
            float value = Mathf.Lerp(from, to, t);

            SetVisual(value);

            time += Time.deltaTime;
            yield return null;
        }

        SetVisual(to);
    }

    // Light 컴포넌트 강도 및 매터리얼 Emission 동기화
    private void SetVisual(float intensity)
    {
        if (targetLight != null)
            targetLight.intensity = intensity;

        if (mat != null && mat.HasProperty("_EmissionColor"))
        {
            Color emission = Color.white * intensity;
            mat.SetColor("_EmissionColor", emission);
        }
    }
}