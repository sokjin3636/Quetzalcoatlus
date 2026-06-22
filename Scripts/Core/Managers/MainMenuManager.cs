using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    [Header("--- UI 패널 ---")]
    public GameObject mainMenuPanel;   // 메인 메뉴 패널 (시작, 설정 등)
    public GameObject settingsPanel;   // 환경설정 패널

    void Start()
    {
        // 씬 시작 시 초기 패널 상태 세팅
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (GameManager.Instance != null)
        {
            Debug.Log($"[MainMenu] 현재 GameManager 상태: {GameManager.Instance.CurrentState}");
        }
    }

    // 시작 버튼 클릭 시 스토리 씬으로 전환
    public void OnStartButtonPressed()
    {
        Debug.Log("[MainMenu] 시작 버튼 클릭. StoryScene으로 전환합니다.");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.Story);
        }
        else
        {
            Debug.LogWarning("[MainMenu] GameManager 인스턴스 부재. SceneManager를 사용합니다.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("StoryScene");
        }
    }

    // 환경설정 버튼 클릭 이벤트
    public void OnSettingsButtonPressed()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        Debug.Log("[MainMenu] 환경설정 패널 활성화");
    }

    // 종료 버튼 이벤트
    public void OnQuitButtonPressed()
    {
        Debug.Log("게임 종료");

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // 환경설정 뒤로가기 버튼 이벤트
    public void OnSettingsBackButtonPressed()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    // 재보정(Recalibration) 버튼 이벤트
    public void OnRecalibrationButtonPressed()
    {
        Debug.Log("[MainMenu] 재보정 요청. 데이터 초기화 후 StartScene으로 이동합니다.");

        // 1. 기존 측정된 캘리브레이션 데이터 초기화
        DataManager.UseHeartRate = false;
        DataManager.BaseAvgBPM = 75f;
        DataManager.BaseRMSSD = 40f;
        DataManager.BaseTremorEnergy = 0.05f;

        if (DataManager.CalibratedRRList != null)
        {
            DataManager.CalibratedRRList.Clear();
        }

        // 2. StartScene으로 이동하여 캘리브레이션 흐름 재시작
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.Start);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("StartScene");
        }
    }
}