using UnityEngine;
using UnityEngine.AI;

public class ZombieInvestigateState : IZombieState
{
    private Vector3 investigateTarget;
    private bool isSearching = false;
    private float searchTimer = 0f;
    private float maxSearchTime;

    private float sightRadius = 2.0f;
    private float sightAngle = 60.0f;
    private float tactileRadius = 1.0f;
    private float hearingRadius = 10.0f;

    private float audioCheckTimer = 0f;
    private float audioCheckInterval = 0.05f;

    private float investigateSpeed = 0.6f;

    private readonly Collider[] hitColliders = new Collider[30];
    private readonly Collider[] safeColliders = new Collider[10];

    private readonly int hearingLayerMask = LayerMask.GetMask("Water");

    public ZombieInvestigateState(Vector3 targetPosition)
    {
        investigateTarget = targetPosition;
        maxSearchTime = Random.Range(5.0f, 7.0f);
    }

    public void Enter(ZombieController zombie)
    {
        isSearching = false;
        searchTimer = 0f;

        investigateTarget = GetSafeDestination(investigateTarget);

        NavMeshAgent agent = zombie.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = investigateSpeed * DataManager.ZombieSpecMultiplier;
            agent.isStopped = false;
            agent.updateRotation = true;
            agent.SetDestination(investigateTarget);
        }

        zombie.Anim.CrossFade("Movement", 0.2f);
    }

    public void Execute(ZombieController zombie)
    {
        if (zombie.target != null)
        {
            float distanceToPlayer = Vector3.Distance(zombie.transform.position, zombie.target.position);

            float currentSightRadius = sightRadius * DataManager.ZombieSpecMultiplier;

            if (distanceToPlayer <= tactileRadius)
            {
                zombie.ChangeState(new ZombieApproachState());
                return;
            }

            if (distanceToPlayer <= currentSightRadius)
            {
                Vector3 dirToPlayerFlat = zombie.target.position - zombie.transform.position;
                dirToPlayerFlat.y = 0;
                dirToPlayerFlat.Normalize();

                Vector3 zombieForwardFlat = zombie.transform.forward;
                zombieForwardFlat.y = 0;
                zombieForwardFlat.Normalize();

                float angle = Vector3.Angle(zombieForwardFlat, dirToPlayerFlat);

                if (angle <= sightAngle * 0.5f)
                {
                    if (CheckLineOfSight(zombie))
                    {
                        zombie.ChangeState(new ZombieApproachState());
                        return;
                    }
                }
            }
        }

        audioCheckTimer += Time.deltaTime;
        if (audioCheckTimer >= audioCheckInterval)
        {
            audioCheckTimer = 0f;

            float currentHearingRadius = hearingRadius * DataManager.ZombieSpecMultiplier;
            int hitCount = Physics.OverlapSphereNonAlloc(zombie.transform.position, currentHearingRadius, hitColliders, hearingLayerMask);

            float closestDist = float.MaxValue;
            Transform bestSoundTransform = null;
            string foundSoundName = "";

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = hitColliders[i];
                if (hitCollider.GetComponentInParent<ZombieController>() != null) continue;

                AudioSource source = hitCollider.GetComponent<AudioSource>();
                if (source == null) source = hitCollider.GetComponentInParent<AudioSource>();

                if (source != null && source.isPlaying && source.volume > 0.01f)
                {
                    if (source.spatialBlend < 0.1f) continue;

                    float distToZombie = Vector3.Distance(zombie.transform.position, hitCollider.transform.position);

                    if (distToZombie < closestDist)
                    {
                        closestDist = distToZombie;
                        bestSoundTransform = hitCollider.transform;
                        foundSoundName = hitCollider.name;
                    }
                }
            }

            if (bestSoundTransform != null)
            {
                Vector3 soundFloorPos = bestSoundTransform.position;
                if (NavMesh.SamplePosition(soundFloorPos, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
                {
                    soundFloorPos = hit.position;
                }

                if (Vector3.Distance(investigateTarget, soundFloorPos) > 1.0f)
                {
                    investigateTarget = GetSafeDestination(soundFloorPos);
                    isSearching = false;
                    searchTimer = 0f;

                    NavMeshAgent agent = zombie.GetComponent<NavMeshAgent>();
                    if (agent != null)
                    {
                        agent.speed = investigateSpeed * DataManager.ZombieSpecMultiplier;
                        agent.isStopped = false;
                        agent.updateRotation = true;
                        agent.SetDestination(investigateTarget);
                    }

                    Debug.Log($"[{zombie.name}] 格利瘤 盎脚凳! 货肺款 家府: {foundSoundName}");
                }
            }
        }

        NavMeshAgent navAgent = zombie.GetComponent<NavMeshAgent>();
        if (navAgent != null)
        {
            if (!isSearching)
            {
                Vector3 flatZombiePos = new Vector3(zombie.transform.position.x, 0, zombie.transform.position.z);
                Vector3 flatTargetPos = new Vector3(investigateTarget.x, 0, investigateTarget.z);

                if (Vector3.Distance(flatZombiePos, flatTargetPos) <= navAgent.stoppingDistance + 0.3f)
                {
                    isSearching = true;
                    navAgent.isStopped = true;
                    navAgent.velocity = Vector3.zero;
                    navAgent.ResetPath();
                    navAgent.updateRotation = false;
                }
            }
            else
            {
                searchTimer += Time.deltaTime;
                if (searchTimer >= maxSearchTime)
                {
                    zombie.ChangeState(new ZombiePatrolState());
                    return;
                }
            }
        }
    }

    public void Exit(ZombieController zombie)
    {
        NavMeshAgent agent = zombie.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.updateRotation = true;
        }
    }

    private bool CheckLineOfSight(ZombieController zombie)
    {
        if (zombie.target == null) return false;

        Vector3 startPos = zombie.transform.position + Vector3.up * 1.6f;
        Vector3 targetPos;

        if (Camera.main != null)
        {
            targetPos = Camera.main.transform.position;
        }
        else
        {
            targetPos = zombie.target.position + Vector3.up * 1.6f;
        }

        Vector3 direction = (targetPos - startPos).normalized;
        float distance = Vector3.Distance(startPos, targetPos);
        float sphereRadius = 0.35f;

        if (Physics.SphereCast(startPos, sphereRadius, direction, out RaycastHit hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.IsChildOf(zombie.transform)) return false;

            if (hit.transform == zombie.target ||
                hit.transform.IsChildOf(zombie.target) ||
                hit.transform.GetComponentInParent<IPlayerSensor>() != null)
            {
                return true;
            }
        }
        return false;
    }

    private Vector3 GetSafeDestination(Vector3 originalPos)
    {
        float safeDistance = 1.5f;

        int hitCount = Physics.OverlapSphereNonAlloc(originalPos, safeDistance, safeColliders);
        for (int i = 0; i < hitCount; i++)
        {
            Collider col = safeColliders[i];
            if (col.CompareTag("Door"))
            {
                Vector3 pushDir = originalPos - col.transform.position;
                pushDir.y = 0f;

                if (pushDir.sqrMagnitude < 0.01f)
                {
                    pushDir = Vector3.forward;
                }

                Vector3 pushedPos = col.transform.position + pushDir.normalized * safeDistance;

                UnityEngine.AI.NavMeshHit hit;
                if (UnityEngine.AI.NavMesh.SamplePosition(pushedPos, out hit, 2.0f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    return hit.position;
                }
            }
        }

        return originalPos;
    }
}