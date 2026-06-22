using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

[System.Serializable]
public class JumpScareScenario
{
    public string scenarioName = "New Scenario";
    public Transform spawnPoint;
    public string scareAnimationName = "";
    public float scareDuration = 1.5f;
    public AudioClip scareSound;
    public GameObject dummyPropToHide;
    public UnityEvent onJumpScareTriggered;
}

[RequireComponent(typeof(Collider))]
public class JumpScareTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    public bool triggerOnlyOnce = true;
    private bool hasTriggered = false;

    [Header("Manual Mode (Shelf)")]
    public bool isManualTriggerMode = false;

    [Header("Two-Way Door Setup")]
    public JumpScareTrigger linkedOppositeTrigger;

    [HideInInspector] public bool isPrimedToFire = false;
    [HideInInspector] public bool hasPrimedOpposite = false;

    private HashSet<Collider> playerCollidersInside = new HashSet<Collider>();
    private Transform lastPlayerTransform;

    [Header("Random Scenarios")]
    public JumpScareScenario[] scenarios;

    // 가비지 컬렉션(GC) 및 순회 비용 절감을 위한 캐싱 변수
    private float cleanupTimer = 0f;
    private static readonly System.Predicate<Collider> IsInvalidCollider = c => c == null || !c.gameObject.activeInHierarchy;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") && !other.transform.root.CompareTag("Player")) return;

        playerCollidersInside.Add(other);
        lastPlayerTransform = other.transform.root;

        if (isManualTriggerMode)
        {
            ShelfNode shelf = GetComponentInParent<ShelfNode>();
            if (shelf != null)
            {
                if (gameObject.name.Contains("A") || transform.parent.name.Contains("A"))
                    shelf.OnPlayerEnterTriggerA();
                else if (gameObject.name.Contains("B") || transform.parent.name.Contains("B"))
                    shelf.OnPlayerEnterTriggerB();
            }
        }

        if (linkedOppositeTrigger != null && !hasPrimedOpposite && !isPrimedToFire)
        {
            hasPrimedOpposite = true;
            linkedOppositeTrigger.isPrimedToFire = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player") && !other.transform.root.CompareTag("Player")) return;
        playerCollidersInside.Remove(other);
    }

    private void Update()
    {
        if (triggerOnlyOnce && hasTriggered) return;

        // 0.5초 주기로 비활성화되거나 파괴된 콜라이더 정리 (성능 최적화)
        cleanupTimer += Time.deltaTime;
        if (cleanupTimer >= 0.5f)
        {
            cleanupTimer = 0f;
            if (playerCollidersInside.Count > 0)
            {
                playerCollidersInside.RemoveWhere(IsInvalidCollider);
            }
        }

        if (playerCollidersInside.Count > 0 && !isManualTriggerMode)
        {
            if (linkedOppositeTrigger == null || isPrimedToFire)
            {
                ActivateJumpScare(lastPlayerTransform);
            }
        }
    }

    public void ActivateJumpScare(Transform playerTransform)
    {
        if (triggerOnlyOnce && hasTriggered) return;
        if (scenarios == null || scenarios.Length == 0) return;

        hasTriggered = true;

        Transform scareGroup = transform.parent;
        if (scareGroup != null)
        {
            JumpScareTrigger[] triggers = scareGroup.GetComponentsInChildren<JumpScareTrigger>(true);
            foreach (var trigger in triggers)
            {
                trigger.hasTriggered = true;
            }
        }

        if (linkedOppositeTrigger != null)
        {
            linkedOppositeTrigger.hasTriggered = true;
            linkedOppositeTrigger.gameObject.SetActive(false);
        }

        int randomIndex = Random.Range(0, scenarios.Length);
        JumpScareScenario pickedScenario = scenarios[randomIndex];

        if (pickedScenario.dummyPropToHide != null) pickedScenario.dummyPropToHide.SetActive(false);

        pickedScenario.onJumpScareTriggered?.Invoke();

        VRMovementSensor playerSensor = Object.FindFirstObjectByType<VRMovementSensor>();
        if (playerSensor != null)
        {
            playerSensor.StartMovementEvent(pickedScenario.scareDuration, 5.0f);
        }

        if (pickedScenario.spawnPoint != null)
        {
            GameObject zombieObj = ZombiePool.Instance.GetZombie(pickedScenario.spawnPoint.position, pickedScenario.spawnPoint.rotation);
            if (zombieObj != null)
            {
                Vector3 exactSpawnPosition = pickedScenario.spawnPoint.position;

                if (Physics.Raycast(pickedScenario.spawnPoint.position + Vector3.up * 1.0f, Vector3.down, out RaycastHit hit, 5.0f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
                {
                    exactSpawnPosition = hit.point;
                }

                if (zombieObj.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent))
                {
                    agent.enabled = false;
                    zombieObj.transform.position = exactSpawnPosition;

                    if (UnityEngine.AI.NavMesh.SamplePosition(exactSpawnPosition, out UnityEngine.AI.NavMeshHit navHit, 2.0f, UnityEngine.AI.NavMesh.AllAreas))
                    {
                        zombieObj.transform.position = navHit.position;
                    }

                    agent.enabled = true;
                }

                if (zombieObj.TryGetComponent<ZombieController>(out var zombie))
                {
                    zombie.transform.rotation = pickedScenario.spawnPoint.rotation;
                    zombie.ChangeState(new ZombieEventState(pickedScenario.scareAnimationName, pickedScenario.scareDuration));
                }
            }
        }

        if (pickedScenario.scareSound != null)
        {
            PlaySoundWithCollider(pickedScenario.scareSound, transform.position);
        }
    }

    private void PlaySoundWithCollider(AudioClip clip, Vector3 pos)
    {
        GameObject soundObj = new GameObject("JumpScareAudio");
        soundObj.transform.position = pos;

        AudioSource audioSource = soundObj.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.spatialBlend = 1.0f;
        audioSource.volume = 1.0f;
        audioSource.Play();

        SphereCollider collider = soundObj.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = 0.5f;

        Destroy(soundObj, clip.length);
    }
}