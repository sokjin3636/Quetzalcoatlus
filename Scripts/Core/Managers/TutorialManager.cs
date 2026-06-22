using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    [Header("--- 튜토리얼 캔버스 ---")]
    [Tooltip("페이지 오브젝트들을 자식으로 가지고 있는 부모 트랜스폼")]
    public Transform pagesParent;

    [Header("--- UI 버튼 ---")]
    public Button prevButton;
    public Button nextButton;

    private int currentPageIndex = 0;
    private int totalPages = 0;

    void Start()
    {
        // 버튼 이벤트 리스너 연결
        if (prevButton != null) prevButton.onClick.AddListener(OnClickPrev);
        if (nextButton != null) nextButton.onClick.AddListener(OnClickNext);

        if (pagesParent == null) pagesParent = this.transform;

        totalPages = pagesParent.childCount;

        UpdatePagesActive();
    }

    // 현재 인덱스에 맞춰 페이지 활성화/비활성화 업데이트
    void UpdatePagesActive()
    {
        if (totalPages == 0) return;

        // 인덱스 클램핑 처리
        currentPageIndex = Mathf.Clamp(currentPageIndex, 0, totalPages - 1);

        for (int i = 0; i < totalPages; i++)
        {
            GameObject pageObj = pagesParent.GetChild(i).gameObject;
            if (pageObj != null)
            {
                pageObj.SetActive(i == currentPageIndex);
            }
        }

        // 첫 페이지 / 마지막 페이지 조건에 따른 버튼 상태 갱신
        if (prevButton != null) prevButton.gameObject.SetActive(currentPageIndex > 0);
        if (nextButton != null) nextButton.gameObject.SetActive(currentPageIndex < totalPages - 1);
    }

    public void OnClickNext()
    {
        if (currentPageIndex < totalPages - 1)
        {
            currentPageIndex++;
            UpdatePagesActive();
            Debug.Log($"[Board] 다음 페이지 이동 ({currentPageIndex + 1}/{totalPages})");
        }
    }

    public void OnClickPrev()
    {
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            UpdatePagesActive();
            Debug.Log($"[Board] 이전 페이지 이동 ({currentPageIndex + 1}/{totalPages})");
        }
    }
}