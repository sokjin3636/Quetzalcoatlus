using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class RandomDecalSpawner : MonoBehaviour
{
    [Header("데칼 프리팹 리스트")]
    public List<GameObject> prefabs = new List<GameObject>();

    [Header("배치 설정")]
    [Range(1, 2000)] public int spawnCount = 100;
    public float minDistance = 1f;

    [Header("위치 및 영역 설정")]
    public float fixedY = 0.01f;
    public Vector3 areaCenter = Vector3.zero;
    public Vector3 areaSize = new Vector3(50f, 0f, 50f);

    [Header("회전 설정")]
    [Tooltip("데칼 정렬을 위한 기본 축 고정 벡터")]
    public Vector3 baseRotation = new Vector3(90f, 0f, 0f);

    [Tooltip("Y축 랜덤 회전의 최소/최대 각도 허용치")]
    public float minYRotation = 0f;
    public float maxYRotation = 360f;

    private int maxRetries = 3000;

    [ContextMenu("1. 데칼 베이킹 (Bake)")]
    public void BakeSpawner()
    {
        if (prefabs == null || prefabs.Count == 0) return;
        GenerateDecals();
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

    public void GenerateDecals()
    {
        ClearSpawner();
        Random.InitState(System.DateTime.Now.Millisecond);

        List<Vector3> placedPositions = new List<Vector3>();
        int attempts = 0;

        while (placedPositions.Count < spawnCount && attempts < maxRetries)
        {
            attempts++;

            Vector3 randomPos = transform.position + new Vector3(
                areaCenter.x + Random.Range(-areaSize.x / 2f, areaSize.x / 2f),
                fixedY,
                areaCenter.z + Random.Range(-areaSize.z / 2f, areaSize.z / 2f)
            );

            bool isTooClose = false;
            if (minDistance > 0f)
            {
                foreach (Vector3 pos in placedPositions)
                {
                    if (Vector3.Distance(randomPos, pos) < minDistance)
                    {
                        isTooClose = true;
                        break;
                    }
                }
            }

            if (!isTooClose)
            {
                placedPositions.Add(randomPos);
                SpawnDecal(randomPos, placedPositions.Count);
            }
        }
    }

    private void SpawnDecal(Vector3 position, int index)
    {
        GameObject selectedPrefab = prefabs[Random.Range(0, prefabs.Count)];
        if (selectedPrefab == null) return;

        GameObject clone;
#if UNITY_EDITOR
        clone = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab, transform);
#else
        clone = Instantiate(selectedPrefab, transform);
#endif

        clone.name = $"{selectedPrefab.name}_{index}";
        clone.transform.position = position;
        clone.hideFlags = HideFlags.None;

#if UNITY_EDITOR
        Undo.RegisterCreatedObjectUndo(clone, "Bake Decals");
#endif

        // X, Z축 고정 및 Y축 무작위 회전값 적용
        float randomY = Random.Range(minYRotation, maxYRotation);
        clone.transform.rotation = Quaternion.Euler(baseRotation.x, baseRotation.y + randomY, baseRotation.z);

        clone.transform.localScale = selectedPrefab.transform.localScale;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
        Vector3 center = transform.position + new Vector3(areaCenter.x, fixedY, areaCenter.z);
        Vector3 size = new Vector3(areaSize.x, 0.1f, areaSize.z);

        Gizmos.DrawCube(center, size);
        Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
        Gizmos.DrawWireCube(center, size);
    }
}