using UnityEngine;
using System.Collections.Generic;

public class TempGameManager : MonoBehaviour
{
    [Header("전체 마트 구역 리스트")]
    public List<ZoneNode> allZones = new List<ZoneNode>();

    [Header("테스트 설정")]
    public int zonesToActivate = 3;

    void Start()
    {
        ExecuteRandomZones();
    }

    [ContextMenu("랜덤 구역 실행 테스트")]
    public void ExecuteRandomZones()
    {
        if (allZones == null || allZones.Count == 0) return;

        int actualCount = Mathf.Min(zonesToActivate, allZones.Count);
        List<ZoneNode> availableZones = new List<ZoneNode>(allZones);
        List<ZoneNode> selectedZones = new List<ZoneNode>();

        // 활성화할 구역(Zone) 무작위 선정
        for (int i = 0; i < actualCount; i++)
        {
            int randomIndex = Random.Range(0, availableZones.Count);
            selectedZones.Add(availableZones[randomIndex]);
            availableZones.RemoveAt(randomIndex);
        }

        Debug.Log($"<color=cyan><b>[TempGameManager] 총 {selectedZones.Count}개의 구역에 스폰 명령을 내립니다.</b></color>");

        // 선정된 구역들에 대한 스폰 명령 일괄 호출
        foreach (ZoneNode zone in selectedZones)
        {
            zone.ProcessZoneSpawn();
        }
    }
}