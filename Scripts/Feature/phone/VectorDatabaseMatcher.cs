using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VectorDatabaseMatcher : MonoBehaviour
{
    [Header("DB")]
    public TextAsset vectorDatabaseJson;

    [Header("Mapper")]
    public SupportToRepresentativeMapper supportMapper;

    [Header("Debug")]
    public bool logLocalFilter = false;

    [Serializable]
    public class VectorEntry
    {
        public string id;
        public string face;
        public float[] vector;
    }

    [Serializable]
    public class VectorEntryWrapper
    {
        public List<VectorEntry> entries;
    }

    [Serializable]
    public class MatchResult
    {
        public string id;
        public string face;
        public float similarity;
    }

    private List<VectorEntry> entries = new List<VectorEntry>();

    void Start()
    {
        LoadDatabase();
    }

    [ContextMenu("Load Database")]
    public void LoadDatabase()
    {
        if (vectorDatabaseJson == null)
        {
            Debug.LogError("vectorDatabaseJson이 연결되지 않았습니다.");
            return;
        }

        VectorEntryWrapper wrapper =
            JsonUtility.FromJson<VectorEntryWrapper>(vectorDatabaseJson.text);

        if (wrapper == null || wrapper.entries == null || wrapper.entries.Count == 0)
        {
            Debug.LogError("벡터 DB 로드 실패 또는 비어 있음");
            return;
        }

        entries = wrapper.entries;

        Debug.Log($"벡터 DB 로드 완료: {entries.Count}개, 차원={entries[0].vector.Length}");
    }

    // 전역 벡터 검색 수행 (코사인 유사도 기반)
    public MatchResult FindBestMatch(float[] queryVector)
    {
        if (entries == null || entries.Count == 0 || queryVector == null || queryVector.Length == 0)
            return null;

        float bestScore = float.NegativeInfinity;
        VectorEntry bestEntry = null;

        foreach (VectorEntry entry in entries)
        {
            if (entry.vector == null || entry.vector.Length != queryVector.Length)
                continue;

            float sim = CosineSimilarity(queryVector, entry.vector);

            if (sim > bestScore)
            {
                bestScore = sim;
                bestEntry = entry;
            }
        }

        if (bestEntry == null) return null;

        return new MatchResult
        {
            id = bestEntry.id,
            face = bestEntry.face,
            similarity = bestScore
        };
    }

    // 지정된 노드 집합(Local Set) 내에서의 제한적 벡터 검색
    public MatchResult FindBestMatchInNodeSet(float[] queryVector, HashSet<string> allowedNodeIds)
    {
        if (entries == null || entries.Count == 0 || queryVector == null || queryVector.Length == 0)
            return null;

        if (allowedNodeIds == null || allowedNodeIds.Count == 0)
        {
            Debug.LogWarning("[Matcher Local] allowedNodeIds가 비어 있음 → 전체 검색 수행");
            return FindBestMatch(queryVector);
        }

        float bestScore = float.NegativeInfinity;
        VectorEntry bestEntry = null;

        int checkedCount = 0;
        int passedCount = 0;

        foreach (VectorEntry entry in entries)
        {
            checkedCount++;
            string repId = ConvertEntryIdToRepresentativeId(entry.id);

            if (string.IsNullOrEmpty(repId) || !allowedNodeIds.Contains(repId))
                continue;

            passedCount++;

            if (entry.vector == null || entry.vector.Length != queryVector.Length)
                continue;

            float sim = CosineSimilarity(queryVector, entry.vector);

            if (sim > bestScore)
            {
                bestScore = sim;
                bestEntry = entry;
            }
        }

        if (logLocalFilter)
        {
            Debug.Log($"[Matcher Local] 전체={checkedCount}, 후보통과={passedCount}, allowedNodeIds={allowedNodeIds.Count}");
        }

        if (bestEntry == null)
        {
            Debug.LogWarning("[Matcher Local] 후보 안에서 일치하는 벡터를 찾지 못함");
            return null;
        }

        return new MatchResult
        {
            id = bestEntry.id,
            face = bestEntry.face,
            similarity = bestScore
        };
    }

    // 상위 K개의 매칭 결과 리스트 반환
    public List<MatchResult> FindTopKMatches(float[] queryVector, int k = 5)
    {
        if (entries == null || entries.Count == 0 || queryVector == null || queryVector.Length == 0)
            return new List<MatchResult>();

        return entries
            .Where(e => e.vector != null && e.vector.Length == queryVector.Length)
            .Select(e => new MatchResult
            {
                id = e.id,
                face = e.face,
                similarity = CosineSimilarity(queryVector, e.vector)
            })
            .OrderByDescending(r => r.similarity)
            .Take(k)
            .ToList();
    }

    private string ConvertEntryIdToRepresentativeId(string entryId)
    {
        if (string.IsNullOrEmpty(entryId)) return "";

        if (supportMapper == null)
        {
            Debug.LogWarning("[Matcher] supportMapper가 연결되지 않았습니다.");
            return "";
        }

        string repId = supportMapper.GetRepresentativeNode(entryId);
        return string.IsNullOrEmpty(repId) ? "" : repId;
    }

    // 다차원 벡터 간의 코사인 유사도 계산 수식
    private float CosineSimilarity(float[] a, float[] b)
    {
        float dot = 0f;
        float magA = 0f;
        float magB = 0f;

        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }

        if (magA <= 1e-8f || magB <= 1e-8f)
            return -1f;

        return dot / (Mathf.Sqrt(magA) * Mathf.Sqrt(magB));
    }
}