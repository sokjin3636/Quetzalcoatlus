using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZombiePool : MonoBehaviour
{
    public static ZombiePool Instance { get; private set; }

    [Header("Pool Settings")]
    [Tooltip("오브젝트 풀링을 위해 등록할 다수의 좀비 프리팹 배열")]
    public GameObject[] zombiePrefabs;
    public int initialPoolSize = 30;

    [Header("Zone Spawning Settings")]
    public Transform[] zoneCenterPoints;
    public int zombiesPerZone = 3;
    public float spawnRadiusPerZone = 8.0f;

    private Queue<GameObject> poolQueue = new Queue<GameObject>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializePool();
    }

    void Start()
    {
        SpawnZombiesInZones();
    }

    private void InitializePool()
    {
        if (zombiePrefabs == null || zombiePrefabs.Length == 0)
        {
            Debug.LogError("ZombiePool: 좀비 프리팹이 등록되지 않았습니다!");
            return;
        }

        for (int i = 0; i < initialPoolSize; i++)
        {
            // 등록된 프리팹 중 무작위 인덱스를 선택하여 풀 큐에 적재
            int randomIndex = Random.Range(0, zombiePrefabs.Length);
            GameObject zombie = Instantiate(zombiePrefabs[randomIndex], transform);

            zombie.SetActive(false);
            poolQueue.Enqueue(zombie);
        }
    }

    private void SpawnZombiesInZones()
    {
        foreach (Transform zone in zoneCenterPoints)
        {
            if (zone == null) continue;

            for (int i = 0; i < zombiesPerZone; i++)
            {
                Vector3 randomDir = Random.insideUnitSphere * spawnRadiusPerZone;
                randomDir += zone.position;

                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomDir, out hit, spawnRadiusPerZone, NavMesh.AllAreas))
                {
                    GetZombie(hit.position, Quaternion.identity);
                }
            }
        }
    }

    public GameObject GetZombie(Vector3 position, Quaternion rotation)
    {
        if (poolQueue.Count > 0)
        {
            GameObject zombie = poolQueue.Dequeue();
            zombie.transform.position = position;
            zombie.transform.rotation = rotation;
            zombie.SetActive(true);
            return zombie;
        }
        Debug.LogWarning("ZombiePool: 풀에 남은 좀비가 없습니다!");
        return null;
    }

    public void ReturnZombie(GameObject zombie)
    {
        zombie.SetActive(false);
        poolQueue.Enqueue(zombie);
    }

    // 에디터 씬 뷰 내 스폰 반경 시각화용 기즈모
    private void OnDrawGizmos()
    {
        if (zoneCenterPoints == null) return;

        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);

        foreach (Transform zone in zoneCenterPoints)
        {
            if (zone != null)
            {
                Gizmos.DrawWireSphere(zone.position, spawnRadiusPerZone);
            }
        }
    }
}