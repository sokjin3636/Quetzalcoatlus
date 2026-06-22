using System.Collections.Generic;
using UnityEngine;

public class ARDirectionArrowRenderer : MonoBehaviour
{
    public NavigationGraphLoader graphLoader;

    [Header("Phone")]
    public Transform phoneTransform;   // 기준이 되는 스마트폰 트랜스폼
    public Transform arrowObject;      // 방향을 지시할 AR 화살표 오브젝트

    [Header("Settings")]
    public float forwardOffset = 0.25f; // 스마트폰 전방 이격 거리
    public float yOffset = 0.02f;
    public bool hideWhenNoPath = true;

    // 경로 노드 리스트를 기반으로 화살표 방향 갱신
    public void ShowDirectionToNextNode(List<string> pathNodeIds)
    {
        if (graphLoader == null || phoneTransform == null || arrowObject == null)
            return;

        if (pathNodeIds == null || pathNodeIds.Count < 2)
        {
            if (hideWhenNoPath)
                arrowObject.gameObject.SetActive(false);

            return;
        }

        arrowObject.gameObject.SetActive(true);

        string nextNodeId = pathNodeIds[1];
        Vector3 nextNodePos = graphLoader.GetNodeWorldPosition(nextNodeId);

        // 화살표 위치를 스마트폰 기준 일정 거리 앞쪽으로 고정
        Vector3 arrowPos =
            phoneTransform.position +
            phoneTransform.forward * forwardOffset +
            phoneTransform.up * yOffset;

        arrowObject.position = arrowPos;

        // 다음 목표 노드를 향한 2D 방향 벡터 산출 (Y축 무시)
        Vector3 dir = nextNodePos - arrowPos;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f)
            return;

        dir.Normalize();

        // 화살표 방향 회전 적용
        arrowObject.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    public void ClearArrow()
    {
        if (arrowObject != null)
            arrowObject.gameObject.SetActive(false);
    }
}