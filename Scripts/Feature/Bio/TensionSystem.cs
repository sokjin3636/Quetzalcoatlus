using UnityEngine;
using System.Collections.Generic;
using System.Text;

public class TensionSystem : MonoBehaviour
{
    [Header("--- 시스템 설정 ---")]
    public bool isCalibrating = false;
    public float calibrationDuration = 30f;
    public float calibrationTimer = 0f;

    [Header("--- 최종 긴장도 ---")]
    [Range(0, 100)] public float currentTension = 0f;
    public float sensitivity = 10f;
    public float baseDecayRate = 1.0f;

    public string currentReasonMessage = "";

    [Header("--- 센서별 가중치 ---")]
    public float weightRMSSD = 1.0f;
    public float weightTremor = 1.0f;

    private ITensionProvider[] tensionProviders;

    // 가비지 컬렉터(GC) 최적화를 위한 변수 캐싱
    private List<string> cachedReasons = new List<string>(10);
    private StringBuilder reasonBuilder = new StringBuilder(100);

    void Start()
    {
        tensionProviders = GetComponents<ITensionProvider>();
        isCalibrating = false;
    }

    public void StartCalibrationProcess()
    {
        if (isCalibrating) return;

        isCalibrating = true;
        calibrationTimer = 0f;
        currentTension = 0f;

        foreach (var provider in tensionProviders)
        {
            provider.StartCalibration();
        }
        Debug.Log("[TensionSystem] 통합 캘리브레이션 시작");
    }

    void Update()
    {
        if (isCalibrating)
        {
            calibrationTimer += Time.deltaTime;

            foreach (var provider in tensionProviders)
            {
                provider.CollectCalibrationData();
            }

            if (calibrationTimer >= calibrationDuration)
            {
                isCalibrating = false;
                foreach (var provider in tensionProviders)
                {
                    provider.FinishCalibration();
                }
                Debug.Log("[TensionSystem] 통합 캘리브레이션 완료 및 기준점 고정");
            }
            return;
        }

        CalculateTension();
    }

    private void CalculateTension()
    {
        if (tensionProviders == null || tensionProviders.Length == 0) return;

        float continuousDelta = 0f;
        float instantTotal = 0f;

        cachedReasons.Clear();

        foreach (var provider in tensionProviders)
        {
            float rawScore = provider.GetRawStressScore();

            // 센서 타입에 따른 텐션 가중치 차등 적용
            if (provider is HeartRateTensionProvider)
            {
                continuousDelta += rawScore * weightRMSSD;
            }
            else if (provider is TremorSensorManager)
            {
                continuousDelta += rawScore * weightTremor;
            }
            else
            {
                continuousDelta += rawScore;
            }

            // 순간 이벤트 점수 가산 
            instantTotal += provider.GetInstantAddition();

            string r = provider.GetActiveReason();
            if (!string.IsNullOrEmpty(r)) cachedReasons.Add(r);
        }

        float changeAmount = (continuousDelta * sensitivity) - (baseDecayRate * Time.deltaTime);

        // 프레임 단위 누적 연산 적용
        currentTension += (changeAmount * Time.deltaTime * 10f) + instantTotal;
        currentTension = Mathf.Clamp(currentTension, 0f, 100f);

        string direction = changeAmount > 0.01f ? "[상승 중] " : (changeAmount < -0.01f ? "[감소 중] " : "[평온 상태] ");

        // 문자열 조합 최적화
        reasonBuilder.Clear();
        reasonBuilder.Append(direction);
        for (int i = 0; i < cachedReasons.Count; i++)
        {
            reasonBuilder.Append(cachedReasons[i]);
            if (i < cachedReasons.Count - 1)
            {
                reasonBuilder.Append(", ");
            }
        }

        currentReasonMessage = reasonBuilder.ToString();
    }
}