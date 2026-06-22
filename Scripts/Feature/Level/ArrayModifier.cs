using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ArrayModifier : MonoBehaviour
{
    [Header("원본 오브젝트")]
    public GameObject prefab;

    [Header("배열 개수")]
    [Range(1, 100)] public int countX = 5;
    [Range(1, 100)] public int countZ = 5;

    [Header("간격 설정 (Base Offset)")]
    public Vector3 constantOffset = new Vector3(1.5f, 0, 1.5f);
    public Vector3 relativeOffset = Vector3.zero;

    [Header("랜덤 설정 (Bake 시 적용)")]
    [Tooltip("각 축별 최대 이동 범위 (좌표 밀림)")]
    public Vector3 positionJitter = new Vector3(0f, 0, 0f);
    [Tooltip("각 축별 최대 회전 범위 (0~360)")]
    public Vector3 maxRotation = new Vector3(0f, 0f, 0f);

    [HideInInspector] public bool isPreview = true;

    [ContextMenu("1. 배열 베이킹 (Bake Array)")]
    public void BakeArray()
    {
        if (prefab == null) return;

        isPreview = false;
        GenerateArray();

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    [ContextMenu("2. 배열 초기화 (Clear)")]
    public void ClearArray()
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

    public void GenerateArray()
    {
        if (prefab == null) return;

        ClearArray();

        Random.InitState(System.DateTime.Now.Millisecond);

        Vector3 objectSize = GetObjectSize(prefab);

        for (int z = 0; z < countZ; z++)
        {
            for (int x = 0; x < countX; x++)
            {
                GameObject clone;
#if UNITY_EDITOR
                clone = (GameObject)PrefabUtility.InstantiatePrefab(prefab, transform);
#else
                clone = Instantiate(prefab, transform);
#endif

                clone.name = $"{prefab.name}_X{x}_Z{z}";

                if (isPreview)
                {
                    clone.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
                }
                else
                {
                    clone.hideFlags = HideFlags.None;
#if UNITY_EDITOR
                    Undo.RegisterCreatedObjectUndo(clone, "Bake Random Array");
#endif
                }

                Vector3 basePos = (new Vector3(constantOffset.x, 0, 0) + new Vector3(objectSize.x * relativeOffset.x, 0, 0)) * x
                                 + (new Vector3(0, 0, constantOffset.z) + new Vector3(0, 0, objectSize.z * relativeOffset.z)) * z
                                 + new Vector3(0, constantOffset.y * (x + z), 0);

                Vector3 jitter = new Vector3(
                    Random.Range(-positionJitter.x, positionJitter.x),
                    Random.Range(-positionJitter.y, positionJitter.y),
                    Random.Range(-positionJitter.z, positionJitter.z)
                );

                clone.transform.localPosition = basePos + jitter;

                Vector3 randomRot = new Vector3(
                    Random.Range(-maxRotation.x, maxRotation.x),
                    Random.Range(-maxRotation.y, maxRotation.y),
                    Random.Range(-maxRotation.z, maxRotation.z)
                );
                clone.transform.localRotation = Quaternion.Euler(randomRot);

                clone.transform.localScale = prefab.transform.localScale;
            }
        }
    }

    Vector3 GetObjectSize(GameObject go)
    {
        Renderer r = go.GetComponentInChildren<Renderer>();
        return r != null ? r.bounds.size : Vector3.one;
    }
}