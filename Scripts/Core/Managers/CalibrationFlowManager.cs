using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class CalibrationFlowManager : MonoBehaviour
{
    [Header("--- UI 패널 및 타임라인 ---")]
    public GameObject bluetoothPanel;
    public PlayableDirector timelineDirector;

    [Header("--- 데이터 참조 ---")]
    public RealHeartRateManager hrManager;
    public TremorSensorManager tremorManager;

    void Start()
    {
        timelineDirector.Stop();

        if (DataManager.UseHeartRate)
        {
            bluetoothPanel.SetActive(true);
        }
        else
        {
            bluetoothPanel.SetActive(false);
            timelineDirector.Play();
        }
    }

    // 타임라인 시그널 1: 데이터 수집 시작
    public void StartAllDataCollection()
    {
        if (hrManager != null) hrManager.StartDataCollection();
        if (tremorManager != null) tremorManager.StartCalibration();
        Debug.Log("[Calibration] 통합 수집 시작");
    }

    // 타임라인 시그널 2: 수집 데이터 정산 및 저장
    public void SaveCalibrationData()
    {
        if (DataManager.UseHeartRate && hrManager != null)
        {
            DataManager.BaseAvgBPM = hrManager.currentBPM;
            DataManager.BaseRMSSD = hrManager.currentRMSSD;
            DataManager.CalibratedRRList = hrManager.GetCurrentRRList();
        }

        if (tremorManager != null)
        {
            DataManager.BaseTremorEnergy = tremorManager.threshold;
        }

        Debug.Log($"[Calibration] 통합 저장 완료");
    }

    // 타임라인 종료 시그널: 인게임 씬 로드
    public void LoadInGameScene()
    {
        if (GameManager.Instance != null)
        {
            // GameManager를 통한 인게임 상태 전환
            GameManager.Instance.ChangeState(GameState.MainMenu);
            Debug.Log("[Calibration] GameManager를 통해 인게임 상태로 전환합니다.");
        }
        else
        {
            // GameManager가 씬에 없을 경우의 예외 처리
            Debug.LogWarning("[Calibration] GameManager가 없어 SceneManager로 인게임을 로드합니다.");
            SceneManager.LoadScene("MainMenuScene");
        }
    }

    // 블루투스 창 완료 버튼 콜백
    public void OnClickBluetoothConnectComplete()
    {
        if (bluetoothPanel != null)
        {
            bluetoothPanel.SetActive(false);
            timelineDirector.Play();
        }
    }
}