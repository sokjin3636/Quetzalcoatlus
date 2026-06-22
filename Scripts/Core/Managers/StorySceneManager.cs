using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;

public class StorySceneManager : MonoBehaviour
{
    [Header("--- VR 스킵 버튼 설정 ---")]
    [Tooltip("스킵에 사용할 인풋 액션을 매핑해주세요.")]
    [SerializeField] private InputActionProperty skipButton;

    [Header("--- 스토리 오디오 및 페이드 설정 ---")]
    [Tooltip("페이드아웃 처리가 필요한 BGM이나 내레이션 AudioSource")]
    [SerializeField] private AudioSource storyAudioSource;
    [Tooltip("페이드아웃 지속 시간 (초)")]
    [SerializeField] private float fadeDuration = 1.5f;

    private bool isSkipped = false; // 중복 스킵 방지 플래그

    void Start()
    {
        if (GameManager.Instance != null)
        {
            Debug.Log($"[StoryScene] 씬 진입. 현재 상태: {GameManager.Instance.CurrentState}");
        }
    }

    void Update()
    {
        // 컨트롤러 스킵 버튼 입력 체크
        if (!isSkipped && skipButton.action != null && skipButton.action.WasPressedThisFrame())
        {
            isSkipped = true;
            Debug.Log("[StoryScene] 스킵 입력 감지. 전환 시작.");

            OnStoryTimelineFinished();
        }
    }

    // 타임라인 연출 종료 또는 스킵 시 공통 호출되는 함수
    public void OnStoryTimelineFinished()
    {
        // 중복 호출 차단
        isSkipped = true;

        Debug.Log("[StoryScene] 연출 종료. 오디오 페이드아웃 및 씬 전환 진행.");

        StartCoroutine(TransitionWithAudioFadeRoutine());
    }

    // 오디오 페이드아웃 후 씬 전환 처리 코루틴
    private IEnumerator TransitionWithAudioFadeRoutine()
    {
        // 1. 오디오 볼륨 페이드아웃
        if (storyAudioSource != null && storyAudioSource.isPlaying)
        {
            float startVolume = storyAudioSource.volume;
            float timer = 0f;

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                storyAudioSource.volume = Mathf.Lerp(startVolume, 0f, timer / fadeDuration);
                yield return null;
            }

            storyAudioSource.volume = 0f;
            storyAudioSource.Stop();
        }

        // 2. 오디오 페이드아웃 완료 후 InGame 씬으로 전환
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.InGame);
        }
        else
        {
            // GameManager가 없을 때의 예외 처리
            Debug.LogWarning("[StoryScene] GameManager 부재로 인한 직접 씬 로드 진행.");
            SceneManager.LoadScene("InGameScene");
        }
    }
}