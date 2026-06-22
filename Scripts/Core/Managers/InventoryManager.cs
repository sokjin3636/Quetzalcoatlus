using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    // 인벤토리에 보관된 아이템 리스트
    public List<string> backpackItems = new List<string>();

    void Awake() => Instance = this;

    public void AddItem(GameObject item)
    {
        if (item == null) return;

        // 동적 생성 시 붙는 (Clone) 문자열 제거
        string cleanName = item.name.Replace("(Clone)", "").Trim();

        backpackItems.Add(cleanName);
        Debug.Log($"[Inventory] 아이템 획득: {cleanName}");

        // 튜토리얼 연동 처리 (아이템 수납 완료 플래그 갱신)
        SimpleTutorialManager tutorialManager = Object.FindFirstObjectByType<SimpleTutorialManager>();
        if (tutorialManager != null)
        {
            tutorialManager.SetItemStoredTrue();
            Debug.Log("[Inventory] 튜토리얼 매니저 아이템 보관 플래그 업데이트 완료.");
        }

        // 아이템 인벤토리 보관 시 맵에서 오브젝트 비활성화
        item.SetActive(false);

        NavigationReceiver nav = Object.FindFirstObjectByType<NavigationReceiver>();
        if (nav != null)
        {
            nav.DisableButtonForItem(cleanName);
        }
    }
}