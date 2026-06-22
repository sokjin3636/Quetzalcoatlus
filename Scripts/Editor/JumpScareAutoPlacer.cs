using UnityEngine;
using UnityEditor;

public class JumpScareAutoPlacer : EditorWindow
{
    [Header("점프스케어 세팅 (높은 매대용)")]
    public GameObject doorJumpScarePrefab;
    public GameObject shelfJumpScarePrefab;

    [Header("시체 투하 세팅 (낮은 매대용)")]
    public GameObject deadCorpsePrefab;
    public AudioClip corpseDropSound;

    [MenuItem("Tools/점프스케어 자동 배치 툴")]
    public static void ShowWindow()
    {
        GetWindow<JumpScareAutoPlacer>("점프스케어 툴");
    }

    private void OnGUI()
    {
        GUILayout.Label("점프스케어 & 시체 투하 자동 세팅", EditorStyles.boldLabel);

        EditorGUILayout.Space();
        GUILayout.Label("[높은 매대용 설정]", EditorStyles.label);
        doorJumpScarePrefab = (GameObject)EditorGUILayout.ObjectField("문(Door) 프리팹", doorJumpScarePrefab, typeof(GameObject), false);
        shelfJumpScarePrefab = (GameObject)EditorGUILayout.ObjectField("가판대(Shelf) 프리팹", shelfJumpScarePrefab, typeof(GameObject), false);

        EditorGUILayout.Space();
        GUILayout.Label("[낮은 매대용 설정]", EditorStyles.label);
        deadCorpsePrefab = (GameObject)EditorGUILayout.ObjectField("투하할 시체 프리팹", deadCorpsePrefab, typeof(GameObject), false);
        corpseDropSound = (AudioClip)EditorGUILayout.ObjectField("시체 낙하 소리", corpseDropSound, typeof(AudioClip), false);

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (GUILayout.Button("맵 전체 문에 자동 배치"))
        {
            if (doorJumpScarePrefab == null)
            {
                EditorUtility.DisplayDialog("오류", "문 점프스케어 프리팹을 넣어주세요.", "확인");
                return;
            }
            AutoPlaceDoorJumpScares();
        }

        if (GUILayout.Button("맵 전체 가판대에 자동 배치 (점프스케어 & 시체)"))
        {
            if (shelfJumpScarePrefab == null)
            {
                EditorUtility.DisplayDialog("오류", "가판대 점프스케어 프리팹을 넣어주세요.", "확인");
                return;
            }
            AutoPlaceShelfJumpScares();
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("맵 전체 점프스케어 일괄 제거 (강력)"))
        {
            if (EditorUtility.DisplayDialog("일괄 제거 경고", "모든 문과 가판대의 점프스케어를 완전히 삭제하시겠습니까?", "네", "취소"))
            {
                RemoveAllJumpScares();
            }
        }
        GUI.backgroundColor = Color.white;
    }

    private void AutoPlaceDoorJumpScares()
    {
        DoorHandleLock[] allDoors = Object.FindObjectsByType<DoorHandleLock>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        int count = 0;

        foreach (DoorHandleLock door in allDoors)
        {
            bool alreadyHasTriggers = false;
            foreach (Transform child in door.transform)
            {
                if (child.name.Contains(doorJumpScarePrefab.name))
                {
                    alreadyHasTriggers = true;
                    break;
                }
            }
            if (alreadyHasTriggers) continue;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(doorJumpScarePrefab);
            instance.transform.SetParent(door.transform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = doorJumpScarePrefab.transform.localRotation;
            count++;
        }
        Debug.Log($"총 {count}개의 문에 배치가 완료되었습니다.");
    }

    private bool IsShortShelfName(string objName)
    {
        return objName.Contains("shoe") || objName.Contains("신발") ||
               objName.Contains("cosmetic") || objName.Contains("화장품") ||
               objName.Contains("meat") || objName.Contains("고기");
    }

    private void AutoPlaceShelfJumpScares()
    {
        ShelfNode[] allShelves = Object.FindObjectsByType<ShelfNode>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        ZoneNode[] allZones = Object.FindObjectsByType<ZoneNode>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        int count = 0;
        int skipCount = 0;

        foreach (ShelfNode shelf in allShelves)
        {
            bool isTall = true;
            string filterReason = "";

            // 1. 오브젝트 하이라키 이름 검사 (낮은 매대 분류용)
            Transform current = shelf.transform;
            while (current != null)
            {
                string objName = current.name.ToLower().Replace(" ", "").Replace("_", "");
                if (IsShortShelfName(objName))
                {
                    isTall = false;
                    filterReason = $"하이라키 이름({current.name})";
                    break;
                }
                current = current.parent;
            }

            // 2. 물리적 Zone 교차 검사
            if (isTall)
            {
                foreach (ZoneNode zone in allZones)
                {
                    BoxCollider zoneCol = zone.GetComponent<BoxCollider>();
                    if (zoneCol != null)
                    {
                        bool isOverlapping = false;
                        Collider[] shelfColliders = shelf.GetComponentsInChildren<Collider>();

                        if (shelfColliders.Length > 0)
                        {
                            foreach (Collider sCol in shelfColliders)
                            {
                                if (zoneCol.bounds.Intersects(sCol.bounds))
                                {
                                    isOverlapping = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            if (zoneCol.bounds.Contains(shelf.transform.position))
                            {
                                isOverlapping = true;
                            }
                        }

                        if (isOverlapping)
                        {
                            string zoneName = zone.gameObject.name.ToLower().Replace(" ", "").Replace("_", "");
                            if (IsShortShelfName(zoneName))
                            {
                                isTall = false;
                                filterReason = $"소속 구역({zone.gameObject.name}) 걸침";
                                break;
                            }
                        }
                    }
                }
            }

            // 낮은 매대로 판별될 경우, 점프스케어 대신 시체 투하 프리팹 및 사운드 할당
            if (!isTall)
            {
                shelf.isTallShelf = false;
                if (deadCorpsePrefab != null) shelf.deadCorpsePrefab = deadCorpsePrefab;
                if (corpseDropSound != null) shelf.corpseDropSound = corpseDropSound;

                EditorUtility.SetDirty(shelf);
                skipCount++;
                Debug.Log($"<color=orange>[낮은 매대 세팅 완료]</color> '{shelf.name}'에 시체 투하가 설정되었습니다. (분류 원인: {filterReason})");
                continue;
            }

            // 높은 매대일 경우 점프스케어 설치 로직
            bool alreadyHasTriggers = false;
            foreach (Transform child in shelf.transform)
            {
                if (shelfJumpScarePrefab != null && child.name.Contains(shelfJumpScarePrefab.name))
                {
                    alreadyHasTriggers = true;
                    break;
                }
            }
            if (alreadyHasTriggers) continue;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(shelfJumpScarePrefab);
            instance.transform.SetParent(shelf.transform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = shelfJumpScarePrefab.transform.localRotation;

            shelf.setA = new JumpScareSet();
            shelf.setB = new JumpScareSet();

            Transform tA = FindChildByName(instance.transform, "TriggerA");
            Transform sA = FindChildByName(instance.transform, "SpawnerA");
            Transform tB = FindChildByName(instance.transform, "TriggerB");
            Transform sB = FindChildByName(instance.transform, "SpawnerB");

            if (tA != null) shelf.setA.approachTrigger = tA.GetComponent<Collider>();
            if (sA != null) shelf.setA.scarePrefab = sA.gameObject;
            if (tB != null) shelf.setB.approachTrigger = tB.GetComponent<Collider>();
            if (sB != null) shelf.setB.scarePrefab = sB.gameObject;

            shelf.AutoAssignTargets();

            // 높은 매대 판별 플래그 갱신
            shelf.isTallShelf = true;
            EditorUtility.SetDirty(shelf);

            count++;
        }
        Debug.Log($"총 {count}개의 높은 매대에 점프스케어 배치가 완료되었습니다. (시체 투하 세팅된 낮은 매대: {skipCount}개)");
    }

    private Transform FindChildByName(Transform parent, string name)
    {
        Transform[] allChildren = parent.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in allChildren)
        {
            if (child.name == name) return child;
        }
        return null;
    }

    private void RemoveAllJumpScares()
    {
        int removedCount = 0;

        DoorHandleLock[] allDoors = Object.FindObjectsByType<DoorHandleLock>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (DoorHandleLock door in allDoors)
        {
            for (int i = door.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = door.transform.GetChild(i);
                if (child.name.Contains("JumpScare") || (doorJumpScarePrefab != null && child.name.Contains(doorJumpScarePrefab.name)))
                {
                    DestroyImmediate(child.gameObject);
                    removedCount++;
                }
            }
        }

        ShelfNode[] allShelves = Object.FindObjectsByType<ShelfNode>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (ShelfNode shelf in allShelves)
        {
            JumpScareTrigger[] triggers = shelf.GetComponentsInChildren<JumpScareTrigger>(true);
            foreach (var t in triggers)
            {
                if (t != null)
                {
                    Transform rootChild = t.transform;
                    while (rootChild.parent != null && rootChild.parent != shelf.transform)
                    {
                        rootChild = rootChild.parent;
                    }

                    if (rootChild.parent == shelf.transform && rootChild.gameObject != null)
                    {
                        DestroyImmediate(rootChild.gameObject);
                        removedCount++;
                    }
                }
            }

            for (int i = shelf.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = shelf.transform.GetChild(i);
                string lowerName = child.name.ToLower();
                if (lowerName.Contains("jumpscare") || lowerName.Contains("trigger") || lowerName.Contains("spawner") ||
                    (shelfJumpScarePrefab != null && child.name.Contains(shelfJumpScarePrefab.name)))
                {
                    DestroyImmediate(child.gameObject);
                    removedCount++;
                }
            }

            shelf.setA = new JumpScareSet();
            shelf.setB = new JumpScareSet();
            // 기존 할당된 시체 데이터는 보존합니다 (초기화하지 않음)

            EditorUtility.SetDirty(shelf);
        }

        Debug.Log($"총 {removedCount}개의 점프스케어 오브젝트가 일괄 제거되었습니다.");
    }
}