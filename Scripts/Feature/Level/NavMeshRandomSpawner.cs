using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class NavMeshRandomSpawner : MonoBehaviour
{
    [Header("원본 오브젝트")]
    public GameObject prefab;

    [Header("배치 설정")]
    [Range(1, 1000)] public int spawnCount = 50;
    [Tooltip("생성 객체 간 최소 유지 거리")]
    public float minDistance = 2f;

    [Tooltip("네비메시 경계면(벽) 최소 이격 거리 설정")]
    public float edgeClearance = 0.5f;

    [Header("영역 설정 (기즈모로 확인)")]
    public Vector3 areaCenter = Vector3.zero;
    public Vector3 areaSize = new Vector3(50f, 10f, 50f);

    [Header("랜덤 설정")]
    public Vector3 maxRotation = new Vector3(0f, 360f, 0f);
    public Vector2 randomScaleRange = new Vector2(0.8f, 1.2f);

    private float maxSampleDistance = 5f;
    private int maxRetries = 2000;

    [ContextMenu("1. NavMesh 베이킹 (Bake)")]
    public void BakeSpawner()
    {
        if (prefab == null) return;
        GenerateOnNavMesh();
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    [ContextMenu("2. 배치 초기화 (Clear)")]
    public void ClearSpawner()
    {
        List<GameObject> children = new List<GameObject>();
        foreach (Transform child in transform) children.Add(child.gameObject);
        foreach (var child in children)
        {
#if UNITY_EDITOR
            Undo.DestroyObjectImmediate(child);
#else
            Destroy(child);
#endif
        }
    }

    public void GenerateOnNavMesh()
    {
        ClearSpawner();
        Random.InitState(System.DateTime.Now.Millisecond);

        List<Vector3> placedPositions = new List<Vector3>();
        int attempts = 0;

        while (placedPositions.Count < spawnCount && attempts < maxRetries)
        {
            attempts++;

            Vector3 randomBoxPoint = transform.position + areaCenter + new Vector3(
                Random.Range(-areaSize.x / 2f, areaSize.x / 2f),
                Random.Range(-areaSize.y / 2f, areaSize.y / 2f),
                Random.Range(-areaSize.z / 2f, areaSize.z / 2f)
            );

            // 무작위 좌표에 대한 네비메시 유효 위치 샘플링
            if (NavMesh.SamplePosition(randomBoxPoint, out NavMeshHit hit, maxSampleDistance, NavMesh.AllAreas))
            {
                // 스폰 지점과 네비메시 경계 간의 최소 거리 확보 검사
                if (NavMesh.FindClosestEdge(hit.position, out NavMeshHit edgeHit, NavMesh.AllAreas))
                {
                    if (edgeHit.distance < edgeClearance)
                    {
                        continue;
                    }
                }

                bool isTooClose = false;
                foreach (Vector3 pos in placedPositions)
                {
                    if (Vector3.Distance(hit.position, pos) < minDistance)
                    {
                        isTooClose = true;
                        break;
                    }
                }

                if (!isTooClose)
                {
                    placedPositions.Add(hit.position);
                    SpawnObject(hit.position, placedPositions.Count);
                }
            }
        }

        Debug.Log($"[NavMesh Spawner] 목표: {spawnCount}개 / 생성: {placedPositions.Count}개");
    }

    private void SpawnObject(Vector3 position, int index)
    {
        GameObject clone;
#if UNITY_EDITOR
        clone = (GameObject)PrefabUtility.InstantiatePrefab(prefab, transform);
#else
        clone = Instantiate(prefab, transform);
#endif

        clone.name = $"{prefab.name}_{index}";
        clone.transform.position = position;
        clone.hideFlags = HideFlags.None;

#if UNITY_EDITOR
        Undo.RegisterCreatedObjectUndo(clone, "Bake NavMesh Spawner");
#endif

        Vector3 randomRot = new Vector3(
            Random.Range(-maxRotation.x, maxRotation.x),
            Random.Range(-maxRotation.y, maxRotation.y),
            Random.Range(-maxRotation.z, maxRotation.z)
        );
        clone.transform.rotation = Quaternion.Euler(randomRot);

        float randomScaleMultiplier = Random.Range(randomScaleRange.x, randomScaleRange.y);
        clone.transform.localScale = prefab.transform.localScale * randomScaleMultiplier;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Gizmos.DrawCube(transform.position + areaCenter, areaSize);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + areaCenter, areaSize);
    }
}