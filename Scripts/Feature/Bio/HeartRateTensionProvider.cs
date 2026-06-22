using UnityEngine;
using System.Collections.Generic;

public class HeartRateTensionProvider : MonoBehaviour, ITensionProvider
{
    public HeartRateManager hrManager;

    [Header("--- 텐션 가중치 설정 ---")]
    public float weightRMSSD = 1.0f;

    [Header("--- 캘리브레이션 결과 데이터 ---")]
    public float baseAvgBPM;
    public float baseRMSSD;

    private List<float> calibRRList = new List<float>();
    private List<int> calibBPMList = new List<int>();
    private float lastRR = 0f;

    public void StartCalibration()
    {
        calibRRList.Clear();
        calibBPMList.Clear();
        lastRR = 0f;

        if (hrManager != null)
        {
            hrManager.StartDataCollection();
        }
    }

    public void CollectCalibrationData()
    {
        if (hrManager == null) return;

        float currentRR = hrManager.currentRR;
        if (currentRR > 0 && currentRR != lastRR)
        {
            lastRR = currentRR;
            calibRRList.Add(currentRR);
        }

        if (hrManager.currentBPM > 0)
        {
            calibBPMList.Add(hrManager.currentBPM);
        }
    }

    public void FinishCalibration()
    {
        if (calibBPMList.Count > 0)
        {
            float sumBPM = 0;
            foreach (var bpm in calibBPMList) sumBPM += bpm;
            baseAvgBPM = sumBPM / calibBPMList.Count;
        }
        else
        {
            baseAvgBPM = 75f;
        }

        if (calibRRList.Count > 1)
        {
            float sumOfSquaredDifferences = 0f;
            for (int i = 0; i < calibRRList.Count - 1; i++)
            {
                float diff = calibRRList[i + 1] - calibRRList[i];
                sumOfSquaredDifferences += diff * diff;
            }

            float meanSquare = sumOfSquaredDifferences / (calibRRList.Count - 1);
            baseRMSSD = Mathf.Sqrt(meanSquare);
        }
        else
        {
            baseRMSSD = 50f;
        }

        Debug.Log($"[HR] RMSSD 캘리브레이션 완료: 기준 RMSSD {baseRMSSD:F1}");
    }

    // RMSSD 하락 비율에 따른 스트레스 수치 연산 (0~1)
    public float GetRawStressScore()
    {
        if (baseRMSSD <= 0 || hrManager == null) return 0f;

        float dropPercent = (baseRMSSD - hrManager.currentRMSSD) / baseRMSSD;
        float rawScore = 0f;

        if (dropPercent >= 0.30f)
        {
            // 임계치(30%) 초과 시 최대 스트레스 산정
            rawScore = 1f;
        }
        else if (dropPercent >= 0.20f)
        {
            // 20~30% 구간 선형 보간 적용
            rawScore = (dropPercent - 0.20f) / (0.30f - 0.20f);
        }
        else
        {
            rawScore = 0f;
        }

        return Mathf.Clamp01(rawScore) * weightRMSSD;
    }

    // BPM 순간 상승 보너스 제외 적용
    public float GetInstantAddition()
    {
        return 0f;
    }

    public string GetActiveReason()
    {
        if (hrManager == null) return "";

        float dropPercent = (baseRMSSD - hrManager.currentRMSSD) / baseRMSSD;
        if (dropPercent >= 0.20f)
        {
            return "심박 변이도(RMSSD) 급감";
        }
        return "";
    }
}