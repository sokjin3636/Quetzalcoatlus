using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System;

public enum GameState
{
    Start,
    Calibration,
    MainMenu,
    Story,
    Tutorial,
    InGame,
    Paused,
    GameOver,
    GameClear
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; }
    public event Action<GameState> OnStateChanged;

    [Header("Fade Settings")]
    public Image fadeImage;
    public float fadeDuration = 1.0f;

    private GameState stateBeforePause;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            StartCoroutine(FadeInRoutine());
        }
    }

    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;
        StartCoroutine(TransitionSceneRoutine(newState));
    }

    private IEnumerator TransitionSceneRoutine(GameState newState)
    {
        yield return StartCoroutine(FadeOutRoutine());

        CurrentState = newState;
        OnStateChanged?.Invoke(newState);

        string targetSceneName = "";
        switch (newState)
        {
            case GameState.Start:
                targetSceneName = "StartScene";
                break;
            case GameState.Calibration:
                targetSceneName = "CalibrationScene";
                break;
            case GameState.MainMenu:
                targetSceneName = "MainMenuScene";
                break;
            case GameState.Story:
                targetSceneName = "StoryScene";
                break;
            case GameState.Tutorial:
                targetSceneName = "TutorialScene";
                break;
            case GameState.InGame:
                targetSceneName = "InGameScene";
                break;
        }

        if (!string.IsNullOrEmpty(targetSceneName))
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
        }

        Time.timeScale = 1f;
        yield return StartCoroutine(FadeInRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        if (fadeImage == null) yield break;

        float timer = 0f;
        Color color = fadeImage.color;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            color.a = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            fadeImage.color = color;
            yield return null;
        }

        color.a = 0f;
        fadeImage.color = color;
    }

    private IEnumerator FadeOutRoutine()
    {
        if (fadeImage == null) yield break;

        float timer = 0f;
        Color color = fadeImage.color;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            color.a = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            fadeImage.color = color;
            yield return null;
        }

        color.a = 1f;
        fadeImage.color = color;
    }

    public void TogglePause()
    {
        if (CurrentState == GameState.InGame)
        {
            stateBeforePause = CurrentState;
            CurrentState = GameState.Paused;
            Time.timeScale = 0f;
            OnStateChanged?.Invoke(CurrentState);
            Debug.Log("[GameManager] Pause Active");
        }
        else if (CurrentState == GameState.Paused)
        {
            CurrentState = stateBeforePause;
            Time.timeScale = 1f;
            OnStateChanged?.Invoke(CurrentState);
            Debug.Log("[GameManager] Pause Deactive");
        }
    }

    public void TriggerGameOver()
    {
        if (CurrentState == GameState.GameOver) return;

        CurrentState = GameState.GameOver;
        Time.timeScale = 0f;
        OnStateChanged?.Invoke(CurrentState);
        Debug.Log("[GameManager] Game Over State Triggered.");
    }

    public void TriggerGameClear()
    {
        if (CurrentState == GameState.GameClear) return;

        CurrentState = GameState.GameClear;
        Time.timeScale = 0f;
        OnStateChanged?.Invoke(CurrentState);
        Debug.Log("[GameManager] Game Clear State Triggered.");
    }
}