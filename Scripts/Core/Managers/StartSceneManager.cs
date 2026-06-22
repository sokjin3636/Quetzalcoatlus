using UnityEngine;
using UnityEngine.SceneManagement;

public class StartSceneManager : MonoBehaviour
{
    void Start()
    {
        // 씬 진입 시 전역 GameManager 상태 디버깅
        if (GameManager.Instance != null)
        {
            Debug.Log($"[StartScene] 매니저 상태 동기화 확인. 현재: {GameManager.Instance.CurrentState}");
        }
    }

    // 심박계 사용 수락 버튼 처리
    public void OnClickYes()
    {
        DataManager.UseHeartRate = true;

        if (GameManager.Instance != null)
        {
            // GameManager를 통한 Calibration 씬 전환
            GameManager.Instance.ChangeState(GameState.Calibration);
        }
        else
        {
            // GameManager 예외 처리
            Debug.LogWarning("[StartScene] GameManager가 없어 직접 로드합니다.");
            SceneManager.LoadScene("Calibration Scene");
        }
    }

    // 심박계 사용 거절 버튼 처리
    public void OnClickNo()
    {
        DataManager.UseHeartRate = false;

        if (GameManager.Instance != null)
        {
            // 심박계를 사용하지 않아도 손떨림 영점 조절을 위해 Calibration 씬으로 이동
            GameManager.Instance.ChangeState(GameState.Calibration);
        }
        else
        {
            Debug.LogWarning("[StartScene] GameManager가 없어 직접 로드합니다.");
            SceneManager.LoadScene("CalibrationScene");
        }
    }
}