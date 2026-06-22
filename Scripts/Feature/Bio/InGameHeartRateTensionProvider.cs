using UnityEngine;
using System.Collections.Generic;

public class InGameHeartRateTensionProvider : MonoBehaviour, ITensionProvider
{
    public InGameRealHeartRateManager hrManager;

    [Header("--- 텐션 가중치 설정 ---")]
    public float weightRMSSD = 1.0f;

    [Header("--- 캘리브레이션 로드 데이터 ---")]
    public float baseAvgBPM;
    public float baseRMSSD;

    void Start()
    {
        // 캘리브레이션 씬 측정치 로드
        baseAvgBPM = DataManager.BaseAvgBPM;
        baseRMSSD = DataManager.BaseRMSSD;

        if (hrManager == null)
        {
            hrManager = GetComponent<InGameRealHeartRateManager>();
        }
    }

    public void StartCalibration() { }
    public void CollectCalibrationData() { }
    public void FinishCalibration() { }

    // RMSSD 하락 비율 기반 스트레스 스코어 계산 로직
    public float GetRawStressScore()
    {
        if (baseRMSSD <= 0 || hrManager == null) return 0f;

        float dropPercent = (baseRMSSD - hrManager.currentRMSSD) / baseRMSSD;
        float rawScore = 0f;

        if (dropPercent >= 0.30f)
        {
            rawScore = 1f;
        }
        else if (dropPercent >= 0.20f)
        {
            rawScore = (dropPercent - 0.20f) / (0.30f - 0.20f);
        }
        else
        {
            rawScore = 0f;
        }

        return Mathf.Clamp01(rawScore) * weightRMSSD;
    }

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