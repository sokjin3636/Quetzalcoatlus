using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class RefrigeratorDoorGenerator : MonoBehaviour
{
    [Header("문 프리팹 리스트")]
    public List<GameObject> doorPrefabs = new List<GameObject>();

    [Header("문 설치 위치")]
    public Transform lDoorAnchor;
    public Transform rDoorAnchor;

    [Header("랜덤 설정")]
    [Range(0f, 100f)] public float closedProbability = 50f;
    [Range(0f, 180f)] public float maxOpenAngle = 45f;
    [Range(0f, 10f)] public float minOpenAngle = 5f;

    [ContextMenu("1. 문 베이킹 (Bake Doors)")]
    public void BakeDoors()
    {
        if (doorPrefabs == null || doorPrefabs.Count == 0) return;

        ClearExistingDoors();

        SpawnDoor(lDoorAnchor, false);
        SpawnDoor(rDoorAnchor, true);

#if UNITY_EDITOR
        Debug.Log($"{gameObject.name}: 문 베이킹 완료.");
        EditorUtility.SetDirty(this);
#endif
    }

    void SpawnDoor(Transform anchor, bool isMirrored)
    {
        GameObject selectedPrefab = doorPrefabs[Random.Range(0, doorPrefabs.Count)];
        if (selectedPrefab == null) return;

        GameObject door;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            // 에디터 환경 내 프리팹 인스턴스 연결 유지 생성
            door = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab);
            door.transform.SetParent(anchor);
            door.transform.localPosition = Vector3.zero;
            door.transform.localRotation = Quaternion.identity;
        }
        else
#endif
        {
            // 런타임 인스턴스화
            door = Instantiate(selectedPrefab, anchor.position, anchor.rotation, anchor);
        }

        door.name = isMirrored ? "Right_Door_Baked" : "Left_Door_Baked";

        door.transform.localScale = isMirrored ? new Vector3(-1, 1, 1) : new Vector3(1, 1, 1);

        float randomAngle = (Random.Range(0f, 100f) > closedProbability)
                            ? Random.Range(minOpenAngle, maxOpenAngle)
                            : 0f;

        float finalAngle = isMirrored ? randomAngle : -randomAngle;
        door.transform.localRotation = Quaternion.Euler(0, finalAngle, 0);

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Undo.RegisterCreatedObjectUndo(door, "Bake Door");
        }
#endif
    }

    [ContextMenu("2. 문 삭제 (Clear Doors)")]
    public void ClearExistingDoors()
    {
        DeleteChildren(lDoorAnchor);
        DeleteChildren(rDoorAnchor);
    }

    void DeleteChildren(Transform parent)
    {
        List<GameObject> children = new List<GameObject>();
        foreach (Transform child in parent) children.Add(child.gameObject);

        foreach (var child in children)
        {
#if UNITY_EDITOR
            Undo.DestroyObjectImmediate(child);
#else
            Destroy(child);
#endif
        }
    }
}