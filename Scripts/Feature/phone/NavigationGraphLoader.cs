using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NavigationGraphLoader : MonoBehaviour
{
    public TextAsset graphJson;

    [Serializable]
    public class NeighborData
    {
        public string id;
        public float distance;
        public float avg_similarity;
        public float direction_similarity;
        public string from_face;
        public string to_face;
    }

    [Serializable]
    public class PositionData
    {
        public float x;
        public float z;
    }

    [Serializable]
    public class NodeData
    {
        public string id;
        public PositionData position;
        public List<NeighborData> neighbors;
    }

    [Serializable]
    public class GraphWrapper
    {
        public List<NodeData> nodes;
    }

    public Dictionary<string, NodeData> nodeMap = new Dictionary<string, NodeData>();

    void Start()
    {
        LoadGraph();
    }

    [ContextMenu("Load Graph")]
    public void LoadGraph()
    {
        if (graphJson == null)
        {
            Debug.LogError("graphJson이 연결되지 않았습니다.");
            return;
        }

        GraphWrapper wrapper = JsonUtility.FromJson<GraphWrapper>(graphJson.text);

        if (wrapper == null || wrapper.nodes == null || wrapper.nodes.Count == 0)
        {
            Debug.LogError("그래프 로드 실패");
            return;
        }

        nodeMap = wrapper.nodes.ToDictionary(n => n.id, n => n);
        Debug.Log($"그래프 로드 완료: 노드 {nodeMap.Count}개");
    }

    public Vector3 GetNodeWorldPosition(string nodeId, float y = 0f)
    {
        if (!nodeMap.ContainsKey(nodeId))
            return Vector3.zero;

        PositionData p = nodeMap[nodeId].position;
        return new Vector3(p.x, y, p.z);
    }

    // 지정 노드로부터 특정 홉(Hop) 거리 이내의 인접 노드 집합 탐색 (BFS)
    public HashSet<string> GetNodesWithinHops(string startNodeId, int hopCount)
    {
        HashSet<string> visited = new HashSet<string>();
        Queue<(string nodeId, int depth)> queue = new Queue<(string nodeId, int depth)>();

        if (string.IsNullOrEmpty(startNodeId) || !nodeMap.ContainsKey(startNodeId))
            return visited;

        visited.Add(startNodeId);
        queue.Enqueue((startNodeId, 0));

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current.depth >= hopCount)
                continue;

            if (!nodeMap.TryGetValue(current.nodeId, out NodeData node))
                continue;

            if (node.neighbors == null)
                continue;

            foreach (NeighborData neighbor in node.neighbors)
            {
                if (neighbor == null || string.IsNullOrEmpty(neighbor.id))
                    continue;

                if (visited.Contains(neighbor.id))
                    continue;

                visited.Add(neighbor.id);
                queue.Enqueue((neighbor.id, current.depth + 1));
            }
        }

        return visited;
    }

    public bool HasNode(string nodeId)
    {
        return !string.IsNullOrEmpty(nodeId) && nodeMap.ContainsKey(nodeId);
    }

    // 특정 월드 좌표와 가장 근접한 노드 탐색
    public string GetNearestNodeId(Vector3 worldPosition)
    {
        string nearestId = "";
        float nearestDistance = float.MaxValue;

        foreach (var kv in nodeMap)
        {
            Vector3 nodePos = GetNodeWorldPosition(kv.Key, worldPosition.y);

            float distance = Vector3.Distance(worldPosition, nodePos);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestId = kv.Key;
            }
        }

        return nearestId;
    }
}