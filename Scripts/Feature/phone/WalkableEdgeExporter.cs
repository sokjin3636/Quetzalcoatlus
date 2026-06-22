using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class WalkableEdge
{
    public string a;
    public string b;
    public float distance;
    public float pathDistance;
}

[System.Serializable]
public class WalkableEdgeWrapper
{
    public List<WalkableEdge> edges = new List<WalkableEdge>();
}

public class WalkableEdgeExporter : MonoBehaviour
{
    [Header("Input Nodes")]
    public Transform supportRoot;

    [Header("Assignment Filter")]
    public ZoneNodeAssigner zoneNodeAssigner;
    public bool useAssignedSupportNodesOnly = true;

    [Header("Representative Node Optimization")]
    [Tooltip("대표 노드 간의 거리가 설정치 이하인 구역의 보조 노드 연결만 계산합니다.")]
    public float maxRepresentativeDistance = 15f;

    [Header("Obstacle Check")]
    public LayerMask wallMask;
    public float[] wallCheckHeights = { 0.4f, 1.0f, 1.6f };

    [Header("Distance")]
    public float maxCheckDistance = 10f;

    [Header("NavMesh")]
    public float maxNavMeshPathMultiplier = 2.0f;
    public float navMeshSampleDistance = 2.0f;

    [Header("Output")]
    public string outputPath = @"C:\Users\PC\Desktop\CD\CapturedCubemaps\walkable_edges.json";

    [Header("Debug")]
    public bool logSkippedReason = false;

    // 내부 연산 전용 캐싱 데이터 구조체
    private struct NodeRuntimeData
    {
        public string panoId;
        public Vector3 navPosition;
        public bool isNavSampleSuccess;
        public Transform repNode;
        public Vector3 repPosition;
    }

    [ContextMenu("Export Walkable Edges")]
    public void ExportWalkableEdges()
    {
        List<ZoneNodeAssigner.SupportNodeAssignment> validAssignments = GetTargetAssignments();

        if (validAssignments.Count == 0)
        {
            Debug.LogError("Walkable edge 계산을 위한 보조 노드 데이터가 없습니다.");
            return;
        }

        int nodeCount = validAssignments.Count;
        WalkableEdgeWrapper wrapper = new WalkableEdgeWrapper();

        int skippedRepDistance = 0;
        int skippedDistance = 0;
        int skippedNoPath = 0;
        int skippedLongPath = 0;
        int skippedWall = 0;
        int skippedSampleFailed = 0;

        // NavMesh 데이터 및 대표 노드 위치 캐싱 처리
        NodeRuntimeData[] cachedNodes = new NodeRuntimeData[nodeCount];
        for (int i = 0; i < nodeCount; i++)
        {
            var assign = validAssignments[i];
            cachedNodes[i].panoId = GetPanoIdFromIndex(i);
            cachedNodes[i].isNavSampleSuccess = TryGetNavMeshPosition(assign.supportNode.position, out cachedNodes[i].navPosition);
            cachedNodes[i].repNode = assign.representativeNode;
            cachedNodes[i].repPosition = assign.representativeNode != null ? assign.representativeNode.position : Vector3.zero;
        }

        // 경로 검증 이중 루프 연산
        for (int i = 0; i < nodeCount; i++)
        {
            // 진행률 UI 렌더링 (에디터 멈춤 현상 완화)
#if UNITY_EDITOR
            if (i % 50 == 0)
            {
                float progress = (float)i / nodeCount;
                if (EditorUtility.DisplayCancelableProgressBar("Walkable Edge Export", $"노드 계산 중... ({i} / {nodeCount})", progress))
                {
                    Debug.LogWarning("작업이 취소되었습니다.");
                    EditorUtility.ClearProgressBar();
                    return;
                }
            }
#endif

            if (!cachedNodes[i].isNavSampleSuccess)
            {
                skippedSampleFailed++;
                continue;
            }

            NodeRuntimeData nodeA = cachedNodes[i];

            for (int j = i + 1; j < nodeCount; j++)
            {
                if (!cachedNodes[j].isNavSampleSuccess) continue;

                NodeRuntimeData nodeB = cachedNodes[j];

                // 대표 노드 기반 인접 구역 필터링
                if (nodeA.repNode != nodeB.repNode)
                {
                    if (nodeA.repNode == null || nodeB.repNode == null ||
                        Vector3.Distance(nodeA.repPosition, nodeB.repPosition) > maxRepresentativeDistance)
                    {
                        skippedRepDistance++;
                        continue;
                    }
                }

                float straightDistance = Vector3.Distance(nodeA.navPosition, nodeB.navPosition);
                if (straightDistance > maxCheckDistance)
                {
                    skippedDistance++;
                    continue;
                }

                if (HasWallBetween(nodeA.navPosition, nodeB.navPosition))
                {
                    skippedWall++;
                    continue;
                }

                NavMeshPath path = new NavMeshPath();
                bool found = NavMesh.CalculatePath(nodeA.navPosition, nodeB.navPosition, NavMesh.AllAreas, path);

                if (!found || path.status != NavMeshPathStatus.PathComplete)
                {
                    skippedNoPath++;
                    continue;
                }

                float pathLength = GetPathLength(path);
                if (pathLength <= 0f)
                {
                    skippedNoPath++;
                    continue;
                }

                // 우회 경로(Detour) 임계치 검사
                if (pathLength > straightDistance * maxNavMeshPathMultiplier)
                {
                    skippedLongPath++;
                    continue;
                }

                if (HasWallAlongPath(path))
                {
                    skippedWall++;
                    continue;
                }

                wrapper.edges.Add(new WalkableEdge
                {
                    a = nodeA.panoId,
                    b = nodeB.panoId,
                    distance = straightDistance,
                    pathDistance = pathLength
                });
            }
        }

#if UNITY_EDITOR
        EditorUtility.ClearProgressBar();
#endif

        string dir = Path.GetDirectoryName(outputPath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(outputPath, json);

        Debug.Log($"<color=green><b>[WalkableEdgeExporter] 저장 완료:</b> {outputPath}</color>");
        Debug.Log($"검사 대상 보조 노드 수: {nodeCount}");
        Debug.Log($"생성된 이동 가능 edge 수: {wrapper.edges.Count}");
        Debug.Log($"[필터링] 대표노드 구역 외 제외: {skippedRepDistance}");
        Debug.Log($"[필터링] 노드 직선거리 초과 제외: {skippedDistance}");
        Debug.Log($"[필터링] NavMesh 보정 실패 제외: {skippedSampleFailed}");
        Debug.Log($"[필터링] NavMesh 경로 없음 제외: {skippedNoPath}");
        Debug.Log($"[필터링] 우회 경로 과다 제외: {skippedLongPath}");
        Debug.Log($"[필터링] 벽 감지 제외: {skippedWall}");
    }

    private bool TryGetNavMeshPosition(Vector3 source, out Vector3 navPos)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(source, out hit, navMeshSampleDistance, NavMesh.AllAreas))
        {
            navPos = hit.position;
            return true;
        }
        navPos = source;
        return false;
    }

    private List<ZoneNodeAssigner.SupportNodeAssignment> GetTargetAssignments()
    {
        List<ZoneNodeAssigner.SupportNodeAssignment> validList = new List<ZoneNodeAssigner.SupportNodeAssignment>();

        if (useAssignedSupportNodesOnly && zoneNodeAssigner != null)
        {
            foreach (var assignment in zoneNodeAssigner.assignments)
            {
                if (assignment == null || assignment.supportNode == null) continue;
                validList.Add(assignment);
            }
            return validList;
        }

        // ZoneNodeAssigner 부재 시 폴백(Fallback) 임시 데이터 구축 로직
        if (supportRoot == null) return validList;
        foreach (Transform child in supportRoot)
        {
            var tempAssign = new ZoneNodeAssigner.SupportNodeAssignment
            {
                supportNode = child,
                representativeNode = null,
                distance = 0f
            };
            validList.Add(tempAssign);
        }
        return validList;
    }

    private bool HasWallBetween(Vector3 a, Vector3 b)
    {
        foreach (float h in wallCheckHeights)
        {
            Vector3 start = a + Vector3.up * h;
            Vector3 end = b + Vector3.up * h;
            Vector3 dir = end - start;
            float dist = dir.magnitude;

            if (dist <= 0.01f) continue;
            if (Physics.Raycast(start, dir.normalized, dist, wallMask)) return true;
        }
        return false;
    }

    private bool HasWallAlongPath(NavMeshPath path)
    {
        if (path == null || path.corners == null || path.corners.Length < 2) return true;

        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            Vector3 a = path.corners[i];
            Vector3 b = path.corners[i + 1];

            foreach (float h in wallCheckHeights)
            {
                Vector3 start = a + Vector3.up * h;
                Vector3 end = b + Vector3.up * h;
                Vector3 dir = end - start;
                float dist = dir.magnitude;

                if (dist <= 0.01f) continue;
                if (Physics.Raycast(start, dir.normalized, dist, wallMask)) return true;
            }
        }
        return false;
    }

    private float GetPathLength(NavMeshPath path)
    {
        float length = 0f;
        if (path == null || path.corners == null || path.corners.Length < 2) return length;

        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            length += Vector3.Distance(path.corners[i], path.corners[i + 1]);
        }
        return length;
    }

    private string GetPanoIdFromIndex(int index)
    {
        return $"pano_{index:D4}";
    }
}