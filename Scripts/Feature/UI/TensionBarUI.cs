using UnityEngine;
using UnityEngine.UI;

public class TensionBarUI : MonoBehaviour
{
    [Header("--- 참조 설정 ---")]
    public InGameTensionSystem tensionSystem;
    public Image fillImage;

    void Start()
    {
        // 인스펙터 누락 대비 Image 컴포넌트 자동 할당
        if (fillImage == null)
        {
            fillImage = GetComponent<Image>();
        }

        // 씬 내 InGameTensionSystem 인스턴스 검색 및 할당
        if (tensionSystem == null)
        {
            tensionSystem = Object.FindFirstObjectByType<InGameTensionSystem>();
        }

        if (tensionSystem == null)
        {
            Debug.LogError("[TensionUI] 씬에 InGameTensionSystem이 존재하지 않습니다!");
        }
    }

    void Update()
    {
        if (tensionSystem == null || fillImage == null) return;

        // 0 ~ 100 범위의 긴장도 수치를 0.0 ~ 1.0 비율로 정규화
        float tensionValue = tensionSystem.currentTension;
        float targetFill = tensionValue / 100f;

        // Image 컴포넌트의 fillAmount 속성에 반영
        fillImage.fillAmount = targetFill;
    }
}