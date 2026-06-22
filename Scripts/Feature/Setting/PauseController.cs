using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseController : MonoBehaviour
{
    [Header("Pause UI")]
    public GameObject pauseCanvas;
    public GameObject pausePanel;
    public GameObject settingsPanel;

    [Header("Scene")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Player View")]
    public Transform cameraTransform;
    public float canvasDistance = 1.5f;

    [Header("Block Other UI During Pause")]
    public CanvasGroup[] otherUICanvasGroups;

    private bool isPaused = false;

    public void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    // 게임 일시정지 및 TimeScale 제어
    public void PauseGame()
    {
        if (isPaused)
            return;

        isPaused = true;
        Time.timeScale = 0f;

        SetOtherUIInteractable(false);

        if (pauseCanvas != null)
            pauseCanvas.SetActive(true);

        if (pausePanel != null)
            pausePanel.SetActive(true);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        MoveCanvasInFrontOfPlayer();
    }

    // 게임 재개 및 TimeScale 복구
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pauseCanvas != null)
            pauseCanvas.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(true);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        SetOtherUIInteractable(true);
    }

    public void OpenSettings()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(true);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SetOtherUIInteractable(true);
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // 일시정지 중 백그라운드 UI 상호작용 차단
    private void SetOtherUIInteractable(bool interactable)
    {
        if (otherUICanvasGroups == null)
            return;

        foreach (CanvasGroup group in otherUICanvasGroups)
        {
            if (group == null)
                continue;

            group.interactable = interactable;
            group.blocksRaycasts = interactable;
        }
    }

    // VR 플레이어의 현재 시야 방향을 기준으로 일시정지 캔버스 위치 재조정
    private void MoveCanvasInFrontOfPlayer()
    {
        if (pauseCanvas == null || cameraTransform == null)
            return;

        pauseCanvas.transform.position =
            cameraTransform.position + cameraTransform.forward * canvasDistance;

        pauseCanvas.transform.rotation =
            Quaternion.LookRotation(
                pauseCanvas.transform.position - cameraTransform.position
            );
    }

    private void OnApplicationQuit()
    {
        Time.timeScale = 1f;
    }
}