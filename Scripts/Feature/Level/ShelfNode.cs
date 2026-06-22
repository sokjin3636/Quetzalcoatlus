using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.AI;

[System.Serializable]
public class JumpScareSet
{
    [Tooltip("플레이어 접근 감지 트리거")]
    public Collider approachTrigger;

    [Tooltip("트리거 작동 시 활성화할 스폰 오브젝트 프리팹")]
    public GameObject scarePrefab;

    [Tooltip("파괴/밀림 연출 대상 전면 가판대")]
    public Rigidbody targetShelfToPush;

    [Tooltip("파괴/밀림 연출 대상 후면 가판대")]
    public Rigidbody backShelfToPush;
}

public class ShelfNode : MonoBehaviour
{
    [Header("스폰 포인트들")]
    public Transform[] spawnPoints;

    [Header("타이밍 설정 (높은 매대 전용)")]
    public float minDelay = 0.5f;
    public float maxDelay = 2.0f;

    [Header("점프스케어 세트 설정 (높은 매대 전용)")]
    public JumpScareSet setA;
    public JumpScareSet setB;

    [Header("시체 투하 설정 (낮은 매대 전용)")]
    public GameObject deadCorpsePrefab;
    public AudioClip corpseDropSound;
    public float dropHeightOffset = 4.3f;

    [Header("디버그 정보")]
    [Tooltip("true일 경우 높은 매대용 점프스케어 트리거 동작, false일 경우 낮은 매대용 시체 투하 동작")]
    public bool isTallShelf = true;

    private JumpScareSet activeSet;

    void Awake()
    {
        if (setA.approachTrigger != null) setA.approachTrigger.gameObject.SetActive(false);
        if (setB.approachTrigger != null) setB.approachTrigger.gameObject.SetActive(false);
    }

    public void SpawnItem(GameObject itemPrefab)
    {
        if (spawnPoints == null || spawnPoints.Length == 0 || itemPrefab == null) return;

        Transform targetPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject spawnedItem = Instantiate(itemPrefab, targetPoint.position, targetPoint.rotation);
        spawnedItem.name = itemPrefab.name;

        Rigidbody rb = spawnedItem.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        ItemPickupRelay relay = spawnedItem.AddComponent<ItemPickupRelay>();
        relay.parentShelf = this;

        XRGrabInteractable grabInteractable = spawnedItem.GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener((args) => relay.OnItemGrabbed());
        }

        if (setA.approachTrigger != null) setA.approachTrigger.gameObject.SetActive(true);
        if (setB.approachTrigger != null) setB.approachTrigger.gameObject.SetActive(true);
    }

    public void OnPlayerEnterTriggerA()
    {
        activeSet = setB;
        if (setA.scarePrefab != null) setA.scarePrefab.SetActive(false);
        if (setB.scarePrefab != null) setB.scarePrefab.SetActive(true);
    }

    public void OnPlayerEnterTriggerB()
    {
        activeSet = setA;
        if (setB.scarePrefab != null) setB.scarePrefab.SetActive(false);
        if (setA.scarePrefab != null) setA.scarePrefab.SetActive(true);
    }

    public void TriggerJumpScare()
    {
        if (isTallShelf)
        {
            // 타겟 선반 객체가 장애물 등에 가려져 참조되지 않은 경우의 예외 처리
            if (activeSet != null && activeSet.targetShelfToPush == null)
            {
                // 이전에 밟은 정상적인 트리거 세트로 롤백 처리
                JumpScareSet originallySteppedSet = (activeSet == setA) ? setB : setA;

                if (originallySteppedSet != null && originallySteppedSet.targetShelfToPush != null)
                {
                    if (activeSet.scarePrefab != null) activeSet.scarePrefab.SetActive(false);
                    activeSet = originallySteppedSet;
                    if (activeSet.scarePrefab != null) activeSet.scarePrefab.SetActive(true);
                    Debug.Log($"[{gameObject.name}] 기둥에 막혀 원래 밟은 세트로 스폰을 교체합니다.");
                }
            }

            float delay = Random.Range(minDelay, maxDelay);
            StartCoroutine(ExecuteScareWithDelay(delay));
        }
        else
        {
            DropCorpseScare();
        }
    }

    private void DropCorpseScare()
    {
        Vector3 dropPosition = transform.position + Vector3.up * dropHeightOffset;

        if (deadCorpsePrefab != null)
        {
            Instantiate(deadCorpsePrefab, dropPosition, Random.rotation);
        }

    }

    private IEnumerator ExecuteScareWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (activeSet != null && activeSet.scarePrefab != null)
        {
            ExecuteCollapseEvent(activeSet.scarePrefab.transform.forward, activeSet.targetShelfToPush, activeSet.backShelfToPush);

            JumpScareTrigger scareTrigger = null;
            if (activeSet.approachTrigger != null)
            {
                scareTrigger = activeSet.approachTrigger.GetComponent<JumpScareTrigger>();
            }

            if (scareTrigger != null)
            {
                Transform playerTransform = Camera.main != null ? Camera.main.transform.root : transform;
                scareTrigger.ActivateJumpScare(playerTransform);
            }
        }
    }

    private void ExecuteCollapseEvent(Vector3 pushDirection, Rigidbody frontRb, Rigidbody backRb)
    {
        Vector3 frontPush = (pushDirection + Vector3.up * 1.0f).normalized;
        Vector3 frontTorque = Vector3.Cross(Vector3.up, pushDirection);
        Vector3 backPush = (pushDirection + Vector3.up * 1.0f).normalized;
        Vector3 backTorque = Vector3.Cross(Vector3.up, pushDirection);

        if (frontRb != null)
        {
            frontRb.isKinematic = false;
            NavMeshObstacle obstacle = frontRb.GetComponent<NavMeshObstacle>();
            if (obstacle != null) obstacle.enabled = false;
            frontRb.AddForce(frontPush * 10f, ForceMode.VelocityChange);
            frontRb.AddTorque(frontTorque * 5f, ForceMode.VelocityChange);
            StartCoroutine(TemporaryGhostMode(frontRb, 0.2f));
        }

        if (backRb != null)
        {
            backRb.isKinematic = false;
            NavMeshObstacle obstacle = backRb.GetComponent<NavMeshObstacle>();
            if (obstacle != null) obstacle.enabled = false;
            backRb.AddForce(backPush * 10f, ForceMode.VelocityChange);
            backRb.AddTorque(backTorque * 5f, ForceMode.VelocityChange);
            StartCoroutine(TemporaryGhostMode(backRb, 0.2f));
        }
    }

    private IEnumerator TemporaryGhostMode(Rigidbody rb, float duration)
    {
        Collider[] colliders = rb.GetComponentsInChildren<Collider>();
        bool originalGravity = rb.useGravity;
        rb.useGravity = false;

        foreach (Collider c in colliders) c.enabled = false;
        yield return new WaitForSeconds(duration);

        if (rb != null)
        {
            foreach (Collider c in colliders) c.enabled = true;
            rb.useGravity = originalGravity;
        }
    }

    [ContextMenu("점프스케어 타겟 자동 연결")]
    public void AutoAssignTargets()
    {
        if (setA.scarePrefab != null)
        {
            setA.targetShelfToPush = FindTargetShelf(setA.scarePrefab.transform);
            if (setA.targetShelfToPush != null) setA.backShelfToPush = FindBackShelf(setA.targetShelfToPush);
        }

        if (setB.scarePrefab != null)
        {
            setB.targetShelfToPush = FindTargetShelf(setB.scarePrefab.transform);
            if (setB.targetShelfToPush != null) setB.backShelfToPush = FindBackShelf(setB.targetShelfToPush);
        }
    }

    // 사방향 레이캐스트를 통한 정면 타겟 가판대(Rigidbody) 탐색 및 장애물 필터링 로직
    private Rigidbody FindTargetShelf(Transform scareTransform)
    {
        Vector3[] checkDirections = { transform.forward, -transform.forward, transform.right, -transform.right };
        Vector3 bestDirection = transform.forward;
        float maxDot = -Mathf.Infinity;

        foreach (Vector3 dir in checkDirections)
        {
            float dot = Vector3.Dot(scareTransform.forward, dir);
            if (dot > maxDot)
            {
                maxDot = dot;
                bestDirection = dir;
            }
        }

        if (Physics.Raycast(scareTransform.position, bestDirection, out RaycastHit hit, 3.0f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
        {
            Rigidbody rb = hit.collider.GetComponentInParent<Rigidbody>();
            if (rb != null) return rb;
        }
        return null;
    }

    private Rigidbody FindBackShelf(Rigidbody frontShelfRb)
    {
        ShelfNode[] allShelves = Object.FindObjectsByType<ShelfNode>(FindObjectsSortMode.None);
        float closestDistance = 3.0f;
        Rigidbody foundBackRb = null;

        foreach (ShelfNode otherShelf in allShelves)
        {
            if (otherShelf.gameObject != frontShelfRb.gameObject && otherShelf.isTallShelf)
            {
                float dist = Vector3.Distance(frontShelfRb.transform.position, otherShelf.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    foundBackRb = otherShelf.GetComponent<Rigidbody>();
                }
            }
        }
        return foundBackRb;
    }

    [ContextMenu("테스트: A면 방향에서 접근 후 물건 집기")]
    public void TestTriggerA()
    {
        if (!Application.isPlaying) return;
        OnPlayerEnterTriggerA();
        TriggerJumpScare();
    }

    [ContextMenu("테스트: B면 방향에서 접근 후 물건 집기")]
    public void TestTriggerB()
    {
        if (!Application.isPlaying) return;
        OnPlayerEnterTriggerB();
        TriggerJumpScare();
    }
}