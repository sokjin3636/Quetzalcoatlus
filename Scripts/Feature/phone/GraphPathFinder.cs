using System.Collections.Generic;
using UnityEngine;

public class GraphPathFinder : MonoBehaviour
{
    public NavigationGraphLoader graphLoader;

    // A* 알고리즘 기반 최단 경로 탐색
    public List<string> FindPath(string startId, string goalId)
    {
        if (graphLoader == null)
            return null;

        if (!graphLoader.HasNode(startId) || !graphLoader.HasNode(goalId))
            return null;

        var openSet = new HashSet<string> { startId };
        var closedSet = new HashSet<string>();

        var cameFrom = new Dictionary<string, string>();
        var gScore = new Dictionary<string, float>();
        var fScore = new Dictionary<string, float>();

        gScore[startId] = 0f;
        fScore[startId] = Heuristic(startId, goalId);

        while (openSet.Count > 0)
        {
            string current = GetLowestFScore(openSet, fScore);

            if (current == goalId)
                return ReconstructPath(cameFrom, current);

            openSet.Remove(current);
            closedSet.Add(current);

            var node = graphLoader.nodeMap[current];

            foreach (var neighbor in node.neighbors)
            {
                if (closedSet.Contains(neighbor.id))
                    continue;

                float currentG = GetScore(gScore, current);
                float neighborG = GetScore(gScore, neighbor.id);

                float tentative = currentG + neighbor.distance;

                if (tentative >= neighborG)
                    continue;

                cameFrom[neighbor.id] = current;
                gScore[neighbor.id] = tentative;
                fScore[neighbor.id] = tentative + Heuristic(neighbor.id, goalId);

                openSet.Add(neighbor.id);
            }
        }

        return null;
    }

    private float GetScore(Dictionary<string, float> scores, string nodeId)
    {
        if (scores.TryGetValue(nodeId, out float value))
            return value;

        return float.MaxValue;
    }

    // 유클리디안 거리 기반 휴리스틱 함수
    private float Heuristic(string a, string b)
    {
        Vector3 pa = graphLoader.GetNodeWorldPosition(a);
        Vector3 pb = graphLoader.GetNodeWorldPosition(b);
        return Vector3.Distance(pa, pb);
    }

    private string GetLowestFScore(HashSet<string> set, Dictionary<string, float> fScore)
    {
        string best = null;
        float bestScore = float.MaxValue;

        foreach (var node in set)
        {
            float score = GetScore(fScore, node);

            if (score < bestScore)
            {
                bestScore = score;
                best = node;
            }
        }

        return best;
    }

    // 도착지 도달 시 노드 역추적을 통한 최종 경로 리스트 구성
    private List<string> ReconstructPath(Dictionary<string, string> cameFrom, string current)
    {
        List<string> path = new List<string>();
        path.Add(current);

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse();
        return path;
    }
}