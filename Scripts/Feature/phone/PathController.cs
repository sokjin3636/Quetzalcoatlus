using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PathController : MonoBehaviour
{
    [Header("Location")]
    public PhoneEmbeddingExtractor extractor;
    public SupportToRepresentativeMapper mapper;

    [Header("Path")]
    public GraphPathFinder pathFinder;
    public string goalNodeId = "RepresentativeNode_05";

    [Header("Preset Destinations")]
    public string destinationA = "RepresentativeNode_05";
    public string destinationB = "RepresentativeNode_12";
    public string destinationC = "RepresentativeNode_20";
    public string destinationD = "RepresentativeNode_30";

    [Header("Renderers")]
    public ARDirectionArrowRenderer arrowRenderer;
    public PhonePathLineRenderer pathLineRenderer;

    [Header("Navigation Button Texts")]
    public Button buttonA;
    public Button buttonB;
    public Button buttonC;
    public Button buttonD;

    [Header("Button Colors")]
    public Color normalButtonColor = Color.white;
    public Color selectedButtonColor = Color.yellow;

    [Header("Update")]
    public float updateInterval = 1f;

    [Header("Off Route")]
    public bool autoRecalculateWhenOffRoute = false;
    public float offRouteTimeToRecalculate = 5f;

    [Header("Stable Guidance")]
    public int requiredStableCount = 3;

    private float nextUpdateTime = 0f;
    private float offRouteTimer = 0f;

    private string currentSupportNode = "";
    private string currentRepresentativeNode = "";

    private string lastObservedRepNode = "";
    private int stableCount = 0;

    private List<string> fixedPath;
    private bool routeActive = false;
    private bool navigationActive = true;
    private int currentPathIndex = 0;

    [Header("Node Arrival")]
    public Transform playerTransform;
    public float nodeArrivalDistance = 2.0f;

    [Header("Initial Position Fix")]
    public bool usePlayerPositionForInitialRoute = true;

    void Update()
    {
        if (!navigationActive) return;

        if (Time.time < nextUpdateTime) return;

        nextUpdateTime = Time.time + updateInterval;

        UpdateCurrentLocation();

        if (!routeActive || fixedPath == null || fixedPath.Count == 0) return;

        UpdateProgressOnFixedPath();
        UpdateGuidance();
    }

    private void UpdateCurrentLocation()
    {
        if (extractor == null || mapper == null) return;

        currentSupportNode = extractor.CurrentNodeId;

        if (string.IsNullOrEmpty(currentSupportNode)) return;

        string repNode = mapper.GetRepresentativeNode(currentSupportNode);

        if (!string.IsNullOrEmpty(repNode))
            currentRepresentativeNode = repNode;
    }

    // 실제 위치와 추정 노드 간의 거리 오차를 고려한 초기 보정
    private string GetCorrectedStartNode()
    {
        string estimatedNode = currentRepresentativeNode;

        if (playerTransform == null || pathFinder == null || pathFinder.graphLoader == null)
            return estimatedNode;

        string nearestNode = pathFinder.graphLoader.GetNearestNodeId(playerTransform.position);

        if (string.IsNullOrEmpty(nearestNode)) return estimatedNode;
        if (string.IsNullOrEmpty(estimatedNode)) return nearestNode;

        Vector3 estimatedPos = pathFinder.graphLoader.GetNodeWorldPosition(estimatedNode, playerTransform.position.y);
        Vector3 nearestPos = pathFinder.graphLoader.GetNodeWorldPosition(nearestNode, playerTransform.position.y);

        float distance = Vector3.Distance(estimatedPos, nearestPos);

        if (distance > 5.0f)
        {
            return nearestNode;
        }

        return estimatedNode;
    }

    public void FindRoute()
    {
        UpdateCurrentLocation();

        if (usePlayerPositionForInitialRoute && playerTransform != null && pathFinder != null)
        {
            string nearestNode = pathFinder.graphLoader.GetNearestNodeId(playerTransform.position);

            if (!string.IsNullOrEmpty(nearestNode))
            {
                currentRepresentativeNode = nearestNode;
                Debug.Log($"초기 위치 보정: {currentRepresentativeNode}");
            }
        }

        if (pathFinder == null) return;

        if (string.IsNullOrEmpty(currentRepresentativeNode))
        {
            Debug.LogWarning("현재 대표 노드를 찾지 못해서 경로를 계산할 수 없습니다.");
            return;
        }

        string startNode = GetCorrectedStartNode();

        fixedPath = pathFinder.FindPath(startNode, goalNodeId);

        if (fixedPath == null || fixedPath.Count == 0)
        {
            Debug.LogWarning($"경로 없음: {currentRepresentativeNode} → {goalNodeId}");
            ClearRoute();
            return;
        }

        routeActive = true;
        currentPathIndex = 0;
        offRouteTimer = 0f;

        lastObservedRepNode = "";
        stableCount = 0;

        Debug.Log($"고정 경로 생성: {currentRepresentativeNode} → {goalNodeId}, 노드 수: {fixedPath.Count}");

        UpdateProgressOnFixedPath();
        UpdateGuidance();
    }

    private void UpdateProgressOnFixedPath()
    {
        if (string.IsNullOrEmpty(currentRepresentativeNode)) return;

        if (currentRepresentativeNode == lastObservedRepNode)
            stableCount++;
        else
        {
            lastObservedRepNode = currentRepresentativeNode;
            stableCount = 1;
        }

        if (stableCount < requiredStableCount) return;

        int index = fixedPath.IndexOf(currentRepresentativeNode);

        if (index >= 0)
        {
            if (index > currentPathIndex)
            {
                currentPathIndex = index;
                Debug.Log($"경로 진행: {currentRepresentativeNode}, index={currentPathIndex}");
            }

            offRouteTimer = 0f;
            return;
        }

        // 경로 노드 이탈 및 다음 노드 직접 도착 검사
        if (playerTransform != null && currentPathIndex < fixedPath.Count - 1)
        {
            for (int i = currentPathIndex + 1; i < fixedPath.Count; i++)
            {
                string nodeId = fixedPath[i];

                Vector3 nodePos = pathFinder.graphLoader.GetNodeWorldPosition(nodeId, playerTransform.position.y);
                float distance = Vector3.Distance(playerTransform.position, nodePos);

                if (distance <= nodeArrivalDistance)
                {
                    currentPathIndex = i;
                    offRouteTimer = 0f;

                    Debug.Log($"노드 건너뛰기 포함 도착 처리: {nodeId}, index={i}, 거리={distance:F2}");
                    return;
                }
            }
        }

        offRouteTimer += updateInterval;
        Debug.LogWarning($"경로 이탈 감지: 현재 {currentRepresentativeNode}, 이탈 시간 {offRouteTimer:F1}s");

        if (autoRecalculateWhenOffRoute && offRouteTimer >= offRouteTimeToRecalculate)
        {
            Debug.LogWarning("경로 이탈 지속 → 자동 재탐색");
            RecalculateRoute();
        }
    }

    private void UpdateGuidance()
    {
        if (fixedPath == null || fixedPath.Count == 0) return;

        if (currentPathIndex >= fixedPath.Count - 1)
        {
            Debug.Log("목적지에 도착했습니다.");
            ClearRoute();
            return;
        }

        List<string> remainingPath = fixedPath.GetRange(currentPathIndex, fixedPath.Count - currentPathIndex);

        if (arrowRenderer != null) arrowRenderer.ShowDirectionToNextNode(remainingPath);
        if (pathLineRenderer != null) pathLineRenderer.ShowPath(remainingPath);
    }

    public void SetGoalNode(string newGoalNodeId)
    {
        goalNodeId = newGoalNodeId;
        ClearRoute();

        Debug.Log($"목적지 변경: {goalNodeId}");
    }

    public void NavigateToA()
    {
        goalNodeId = destinationA;
        ClearRoute();
        HighlightNavigationButton(buttonA);
        FindRoute();
    }

    public void NavigateToB()
    {
        goalNodeId = destinationB;
        ClearRoute();
        HighlightNavigationButton(buttonB);
        FindRoute();
    }

    public void NavigateToC()
    {
        goalNodeId = destinationC;
        ClearRoute();
        HighlightNavigationButton(buttonC);
        FindRoute();
    }

    public void NavigateToD()
    {
        goalNodeId = destinationD;
        ClearRoute();
        HighlightNavigationButton(buttonD);
        FindRoute();
    }

    private void HighlightNavigationButton(Button selectedButton)
    {
        ResetNavigationButtonColors();

        if (selectedButton != null)
            selectedButton.image.color = selectedButtonColor;
    }

    private void ResetNavigationButtonColors()
    {
        if (buttonA != null) buttonA.image.color = normalButtonColor;
        if (buttonB != null) buttonB.image.color = normalButtonColor;
        if (buttonC != null) buttonC.image.color = normalButtonColor;
        if (buttonD != null) buttonD.image.color = normalButtonColor;
    }

    public void ClearRoute()
    {
        routeActive = false;
        fixedPath = null;
        currentPathIndex = 0;
        offRouteTimer = 0f;

        lastObservedRepNode = "";
        stableCount = 0;

        if (arrowRenderer != null) arrowRenderer.ClearArrow();
        if (pathLineRenderer != null) pathLineRenderer.ClearLine();

        Debug.Log("경로 초기화");
    }

    public void RecalculateRoute()
    {
        ClearRoute();
        FindRoute();
    }

    public void SetNavigationActive(bool active)
    {
        navigationActive = active;

        if (!navigationActive)
        {
            if (arrowRenderer != null) arrowRenderer.ClearArrow();
            if (pathLineRenderer != null) pathLineRenderer.ClearLine();
        }
        else
        {
            if (routeActive) UpdateGuidance();
        }
    }

    public void ClearSelectedButton()
    {
        ResetNavigationButtonColors();
    }
}