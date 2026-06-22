using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameResultCanvasController : MonoBehaviour
{
    [Header("UI")]
    public Canvas resultCanvas;
    public Image backgroundImage;
    public TMP_Text titleText;
    public TMP_Text subText;
    public GameObject staminaUI;

    [Header("Fade")]
    public float fadeDuration = 1.5f;

    [Range(0f, 1f)]
    public float targetAlpha = 0.85f;

    [Header("Messages")]
    public string gameOverTitle = "GAME OVER";
    public string gameOverSubText = "좀비에게 붙잡혔습니다";

    public string clearTitle = "CLEAR";
    public string clearSubText = "탈출에 성공했습니다";

    [Header("Colors")]
    public Color gameOverColor = Color.red;
    public Color clearColor = Color.green;

    private Coroutine showRoutine;

    private void Start()
    {
        HideImmediate();

        // GameManager의 상태 전이 이벤트 구독
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += HandleStateChanged;
        }
    }

    private void OnDestroy()
    {
        // 메모리 누수 방지를 위한 이벤트 구독 해제
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
        }
    }

    // GameState 변경에 따른 결과 화면 출력 분기
    private void HandleStateChanged(GameState newState)
    {
        if (newState == GameState.GameOver)
        {
            ShowGameOver();
        }
        else if (newState == GameState.GameClear)
        {
            ShowClear();
        }
    }

    public void ShowGameOver()
    {
        ShowResult(gameOverTitle, gameOverSubText, gameOverColor);
    }

    public void ShowClear()
    {
        ShowResult(clearTitle, clearSubText, clearColor);
    }

    private void ShowResult(string title, string sub, Color color)
    {
        if (staminaUI != null)
            staminaUI.SetActive(false);

        if (showRoutine != null)
            StopCoroutine(showRoutine);

        Time.timeScale = 0f;

        if (resultCanvas != null)
            resultCanvas.enabled = true;

        if (titleText != null)
        {
            titleText.text = title;
            titleText.alpha = 0f;
        }

        if (subText != null)
        {
            subText.text = sub;
            subText.alpha = 0f;
        }

        showRoutine = StartCoroutine(FadeRoutine(color));
    }

    private IEnumerator FadeRoutine(Color baseColor)
    {
        float time = 0f;

        Color bgColor = baseColor;
        bgColor.a = 0f;

        if (backgroundImage != null)
            backgroundImage.color = bgColor;

        // 1. UI 알파값 보간(Lerp)을 통한 페이드 인 연출
        while (time < fadeDuration)
        {
            float t = time / fadeDuration;
            float alpha = Mathf.Lerp(0f, targetAlpha, t);

            if (backgroundImage != null)
            {
                bgColor.a = alpha;
                backgroundImage.color = bgColor;
            }

            if (titleText != null)
                titleText.alpha = t;

            if (subText != null)
                subText.alpha = t;

            time += Time.unscaledDeltaTime;
            yield return null;
        }

        if (backgroundImage != null)
        {
            bgColor.a = targetAlpha;
            backgroundImage.color = bgColor;
        }

        if (titleText != null)
            titleText.alpha = 1f;

        if (subText != null)
            subText.alpha = 1f;

        // 2. 결과 텍스트 출력 후 지연 시간 대기
        float waitTime = 3f;
        float waitTimer = 0f;
        while (waitTimer < waitTime)
        {
            waitTimer += Time.unscaledDeltaTime;
            yield return null;
        }

        // 3. 지연 완료 후 메인 메뉴 씬 전환 호출
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.MainMenu);
        }
    }

    public void HideImmediate()
    {
        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
        }

        if (resultCanvas != null)
            resultCanvas.enabled = false;

        if (backgroundImage != null)
        {
            Color c = backgroundImage.color;
            c.a = 0f;
            backgroundImage.color = c;
        }

        if (titleText != null)
            titleText.alpha = 0f;

        if (subText != null)
            subText.alpha = 0f;

        if (staminaUI != null)
            staminaUI.SetActive(true);
    }

    public void ResumeTime()
    {
        Time.timeScale = 1f;
    }
}