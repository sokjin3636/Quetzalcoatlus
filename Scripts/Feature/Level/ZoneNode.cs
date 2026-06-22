using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider))]
public class ZoneNode : MonoBehaviour
{
    [Header("이 구역 전용 아이템 리스트")]
    public List<GameObject> itemPool = new List<GameObject>();

    [Header("디버그 설정")]
    [Tooltip("스폰 타겟으로 선정된 선반 오브젝트의 하이라이트(Blink) 효과 토글")]
    public bool enableDebugBlink = true;

    public string ProcessZoneSpawn()
    {
        GameObject selectedItem = null;
        string itemName = "";

        if (itemPool != null && itemPool.Count > 0)
        {
            selectedItem = itemPool[Random.Range(0, itemPool.Count)];
            itemName = selectedItem.name;
        }
        else
        {
            Debug.Log($"[{gameObject.name}] 아이템 리스트가 비어있어 빈 손으로 선반을 찾습니다.");
            return "";
        }

        BoxCollider zoneCollider = GetComponent<BoxCollider>();
        Vector3 center = transform.TransformPoint(zoneCollider.center);
        Vector3 halfExtents = Vector3.Scale(zoneCollider.size, transform.lossyScale) * 0.5f;

        Collider[] hits = Physics.OverlapBox(center, halfExtents, transform.rotation);
        List<ShelfNode> availableShelves = new List<ShelfNode>();

        foreach (Collider hit in hits)
        {
            Transform current = hit.transform;
            while (current != null)
            {
                if (current.CompareTag("Shelf"))
                {
                    ShelfNode shelf = current.GetComponent<ShelfNode>();
                    if (shelf != null && !availableShelves.Contains(shelf))
                    {
                        availableShelves.Add(shelf);
                    }
                    break;
                }
                current = current.parent;
            }
        }

        if (availableShelves.Count > 0)
        {
            ShelfNode finalShelf = availableShelves[Random.Range(0, availableShelves.Count)];

            Debug.Log($"[{gameObject.name}] 구역 -> [{finalShelf.name}] 선반 선택됨! (스폰 아이템: {itemName})");

            if (enableDebugBlink && Application.isPlaying && finalShelf.GetComponent<BlinkHighlight>() == null)
            {
                finalShelf.gameObject.AddComponent<BlinkHighlight>();
            }

            finalShelf.SpawnItem(selectedItem);

            // 구역 중심이 아닌 실제 아이템이 스폰된 선반의 위치 정보를 이벤트 파라미터로 전달
            if (GameEventManager.OnItemSpawnedInZone != null)
            {
                GameEventManager.OnItemSpawnedInZone.Invoke(finalShelf.transform.position, itemName);
            }

            return itemName;
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] 구역 내부에 스폰 가능한 선반이 없습니다.");
            return "";
        }
    }
}

public class BlinkHighlight : MonoBehaviour
{
    private Renderer[] renderers;
    private Color[] originalColors;
    private float blinkSpeed = 5f;

    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material.HasProperty("_Color")) originalColors[i] = renderers[i].material.color;
            else if (renderers[i].material.HasProperty("_BaseColor")) originalColors[i] = renderers[i].material.GetColor("_BaseColor");
        }
    }

    void Update()
    {
        float lerp = Mathf.PingPong(Time.time * blinkSpeed, 1f);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material.HasProperty("_Color")) renderers[i].material.color = Color.Lerp(originalColors[i], Color.yellow, lerp);
            else if (renderers[i].material.HasProperty("_BaseColor")) renderers[i].material.SetColor("_BaseColor", Color.Lerp(originalColors[i], Color.yellow, lerp));
        }
    }
}