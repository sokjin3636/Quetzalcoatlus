using UnityEngine;
using UnityEngine.UI;

public class TensionUIManager : MonoBehaviour
{
    public TensionSystem tensionSystem;
    public HeartRateTensionProvider hrProvider;
    private TremorSensorManager tremorProvider;

    [Header("UI 연결")]
    public Button calibButton;
    public Text statusText;
    public Text resultText;
    public Text tensionText;
    public Text reasonText;

    private bool hasCalibratedOnce = false;

    void Start()
    {
        calibButton.onClick.AddListener(OnCalibButtonClick);

        tremorProvider = tensionSystem.GetComponent<TremorSensorManager>();

        statusText.text = "준비 완료";
        tensionText.text = "";
    }

    void Update()
    {
        if (tensionSystem.isCalibrating)
        {
            hasCalibratedOnce = true;
            float r = tensionSystem.calibrationDuration - tensionSystem.calibrationTimer;
            statusText.text = $"측정 중... {r:F1}초";
            tensionText.text = "";
            reasonText.text = "";
        }
        else if (hasCalibratedOnce)
        {
            statusText.text = "측정 완료";

            resultText.text = $"기준 BPM: {hrProvider.baseAvgBPM:F0} / RMSSD: {hrProvider.baseRMSSD:F1}";

            // 가중치가 적용되지 않은 Raw Score 산출
            float hrRaw = hrProvider.GetRawStressScore();
            float tremorRaw = (tremorProvider != null) ? tremorProvider.GetRawStressScore() : 0f;

            // 긴장도 및 센서별 Raw Score UI 갱신
            tensionText.text = $"긴장도: {tensionSystem.currentTension:F1}\n" +
                              $"<size=15>(Raw) HR: {hrRaw:F2} / Tremor: {tremorRaw:F2}</size>";

            reasonText.text = tensionSystem.currentReasonMessage;
        }
    }

    void OnCalibButtonClick()
    {
        tensionSystem.StartCalibrationProcess();
    }
}