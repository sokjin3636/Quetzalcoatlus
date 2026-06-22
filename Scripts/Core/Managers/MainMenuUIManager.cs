using UnityEngine;
using UnityEngine.UI;

public class MainMenuUIManager : MonoBehaviour
{
    [Header("--- 데이터 표시용 UI ---")]
    public Text bpmText;
    public Text rmssdText;

    void Start()
    {
        // DataManager에 저장된 최종 캘리브레이션 결과값을 UI 텍스트에 적용
        if (bpmText != null)
        {
            bpmText.text = $"측정된 기준 BPM: {DataManager.BaseAvgBPM:F0}";
        }

        if (rmssdText != null)
        {
            rmssdText.text = $"측정된 기준 RMSSD: {DataManager.BaseRMSSD:F1}";
        }

        Debug.Log($"[MainMenu] DataManager 연동 - BPM: {DataManager.BaseAvgBPM}, RMSSD: {DataManager.BaseRMSSD}");
    }
}