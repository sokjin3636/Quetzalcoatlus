using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    [Header("마트 구역 설정 (TempGameManager 기능 통합)")]
    public List<ZoneNode> allZones = new List<ZoneNode>();
    public int zonesToActivate = 3;

    [Header("이번 게임의 필수 수집 아이템 (자동 할당됨)")]
    public List<string> requiredItems = new List<string>();

    void Awake()
    {
        Instance = this;
    }

    IEnumerator Start()
    {
        // 스마트폰 UI 및 내비게이션 노드 초기화 대기
        yield return new WaitForSeconds(0.1f);

        AssignQuestItemsFromZones();
    }

    private void AssignQuestItemsFromZones()
    {
        if (allZones == null || allZones.Count == 0) return;

        requiredItems.Clear();

        int actualCount = Mathf.Min(zonesToActivate, allZones.Count);
        List<ZoneNode> availableZones = new List<ZoneNode>(allZones);
        List<ZoneNode> selectedZones = new List<ZoneNode>();

        // 활성화할 스폰 구역 무작위 선정
        for (int i = 0; i < actualCount; i++)
        {
            int randomIndex = Random.Range(0, availableZones.Count);
            selectedZones.Add(availableZones[randomIndex]);
            availableZones.RemoveAt(randomIndex);
        }

        Debug.Log($"<color=cyan><b>[QuestManager] 총 {selectedZones.Count}개의 구역에 퀘스트 아이템 스폰을 명령합니다.</b></color>");

        // 선정된 구역 객체에 스폰 명령 전달 및 반환된 아이템을 퀘스트 목표 리스트에 등록
        foreach (ZoneNode zone in selectedZones)
        {
            string spawnedItemName = zone.ProcessZoneSpawn();

            if (!string.IsNullOrEmpty(spawnedItemName))
            {
                requiredItems.Add(spawnedItemName);
            }
        }

        Debug.Log("[QuestManager] 이번 생존 퀘스트 목표 확정: " + string.Join(", ", requiredItems));
    }

    // 인벤토리 데이터를 통한 필수 수집 아이템 확보 여부 검증
    public bool IsQuestComplete()
    {
        if (InventoryManager.Instance == null) return false;

        foreach (string item in requiredItems)
        {
            if (!InventoryManager.Instance.backpackItems.Contains(item))
            {
                return false;
            }
        }
        return true;
    }
}