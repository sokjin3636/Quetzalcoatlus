using UnityEngine;
using UnityEngine.AI;

public class ZombiePatrolState : IZombieState
{
    private float patrolRadius = 10f;

    private float minWaitTime = 1.0f;
    private float maxWaitTime = 6.0f;
    private float currentWaitTime = 3.0f;

    private float timer = 0f;
    private bool isWaiting = false;

    private float ignorePlayerTimer = 0f;

    private float moanTimer = 0f;
    private float nextMoanTime = 0f;

    private bool idleStandSoundPlayed = false;

    private int lastMoanIndex = -1;
    private int lastStandIndex = -1;

    private float walkTimer = 0f;
    private float maxWalkTime = 15.0f;

    private float audioCheckTimer = 0f;
    private float audioCheckInterval = 0.2f;

    private float visionCheckTimer = 0f;
    private float visionCheckInterval = 0.15f;

    private float sightRadius = 6.0f;
    private float sightAngle = 60.0f;

    private float tactileRadius = 1.0f;
    private float hearingRadius = 15.0f;

    private float visionSphereRadius = 0.35f;

    private readonly Collider[] hitColliders = new Collider[30];
    private readonly Collider[] safeColliders = new Collider[10];

    private readonly int hearingLayerMask = LayerMask.GetMask("Water");

    public ZombiePatrolState(float gracePeriod = 0f)
    {
        ignorePlayerTimer = gracePeriod;
    }

    public void Enter(ZombieController zombie)
    {
        isWaiting = false;
        idleStandSoundPlayed = false;

        currentWaitTime = Random.Range(minWaitTime, maxWaitTime);

        zombie.Anim.CrossFade("Movement", 0.2f);

        NavMeshAgent agent = zombie.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            // 전역 동적 난이도 계수에 따른 이동 속도 적용
            agent.speed = 0.3f * DataManager.ZombieSpecMultiplier;
            agent.isStopped = false;
            agent.updateRotation = true;
        }

        SetRandomDestination(zombie);
        SetNextMoanTime();

        Debug.Log($"[{zombie.name}] Patrol Enter");
    }

    public void Execute(ZombieController zombie)
    {
        if (zombie.target == null)
        {
            if (Camera.main != null)
            {
                zombie.target = Camera.main.transform.root;
                Debug.Log($"[{zombie.name}] Target Reassigned");
            }
        }

        if (ignorePlayerTimer > 0f)
        {
            ignorePlayerTimer -= Time.deltaTime;
        }
        else
        {
            visionCheckTimer += Time.deltaTime;
            if (visionCheckTimer >= visionCheckInterval)
            {
                visionCheckTimer = 0f;

                if (zombie.target != null)
                {
                    Vector3 playerPos = GetPlayerPosition();
                    float distanceToPlayer = Vector3.Distance(zombie.transform.position, playerPos);

                    float currentSightRadius = sightRadius * DataManager.ZombieSpecMultiplier;

                    if (distanceToPlayer <= tactileRadius)
                    {
                        Debug.Log($"[{zombie.name}] Tactile Detect");
                        zombie.ChangeState(new ZombieApproachState());
                        return;
                    }

                    if (distanceToPlayer <= currentSightRadius)
                    {
                        Vector3 dirToPlayerFlat = playerPos - zombie.transform.position;
                        dirToPlayerFlat.y = 0f;
                        dirToPlayerFlat.Normalize();

                        Vector3 zombieForwardFlat = zombie.transform.forward;
                        zombieForwardFlat.y = 0f;
                        zombieForwardFlat.Normalize();

                        float angle = Vector3.Angle(zombieForwardFlat, dirToPlayerFlat);

                        if (angle <= sightAngle * 0.5f)
                        {
                            bool hasLOS = CheckLineOfSight(zombie, playerPos);
                            if (hasLOS)
                            {
                                Debug.Log($"[{zombie.name}] Visual Detect");
                                zombie.ChangeState(new ZombieApproachState());
                                return;
                            }
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

                for (int i = 0; i < hitCount; i++)
                {
                    Collider hitCollider = hitColliders[i];

                    if (hitCollider.GetComponentInParent<ZombieController>() != null)
                        continue;

                    AudioSource source = hitCollider.GetComponent<AudioSource>();
                    if (source == null)
                    {
                        source = hitCollider.GetComponentInParent<AudioSource>();
                    }

                    if (source != null && source.isPlaying && source.volume > 0.01f)
                    {
                        Vector3 newTarget = hitCollider.transform.position;

                        if (NavMesh.SamplePosition(newTarget, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
                        {
                            newTarget = hit.position;
                        }

                        Debug.Log($"[{zombie.name}] Heard Sound : {hitCollider.name}");
                        zombie.ChangeState(new ZombieInvestigateState(newTarget));
                        return;
                    }
                }
            }
        }

        NavMeshAgent navAgent = zombie.GetComponent<NavMeshAgent>();
        if (navAgent != null)
        {
            if (isWaiting)
            {
                timer += Time.deltaTime;

                if (timer >= 3f && !idleStandSoundPlayed)
                {
                    PlayRandomIdleStandSound(zombie);
                    idleStandSoundPlayed = true;
                }

                if (timer >= currentWaitTime)
                {
                    isWaiting = false;

                    idleStandSoundPlayed = false;

                    navAgent.isStopped = false;
                    navAgent.updateRotation = true;

                    currentWaitTime = Random.Range(minWaitTime, maxWaitTime);

                    SetRandomDestination(zombie);
                }
            }
            else
            {
                moanTimer += Time.deltaTime;

                if (moanTimer >= nextMoanTime)
                {
                    PlayRandomMoan(zombie);
                    SetNextMoanTime();
                }

                walkTimer += Time.deltaTime;
                bool hasReached = false;

                if (!navAgent.pathPending)
                {
                    if (navAgent.remainingDistance <= navAgent.stoppingDistance)
                    {
                        hasReached = true;
                    }
                    else if (!navAgent.hasPath)
                    {
                        hasReached = true;
                    }
                    else
                    {
                        Vector3 flatZombiePos = new Vector3(zombie.transform.position.x, 0, zombie.transform.position.z);
                        Vector3 flatDestPos = new Vector3(navAgent.destination.x, 0, navAgent.destination.z);

                        if (Vector3.Distance(flatZombiePos, flatDestPos) <= navAgent.stoppingDistance + 0.3f)
                        {
                            hasReached = true;
                        }
                    }
                }

                if (walkTimer >= maxWalkTime)
                {
                    hasReached = true;
                }

                if (hasReached)
                {
                    isWaiting = true;
                    timer = 0f;
                    idleStandSoundPlayed = false;

                    currentWaitTime = Random.Range(minWaitTime, maxWaitTime);

                    navAgent.isStopped = true;
                    navAgent.velocity = Vector3.zero;
                    navAgent.ResetPath();
                    navAgent.updateRotation = false;
                }
            }
        }
    }

    public void Exit(ZombieController zombie)
    {
        NavMeshAgent agent = zombie.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.isStopped = true;
            agent.updateRotation = true;
        }

        if (zombie.moveAudioSource != null)
        {
            zombie.moveAudioSource.Stop();
        }

        Debug.Log($"[{zombie.name}] Patrol Exit");
    }

    private Vector3 GetPlayerPosition()
    {
        if (Camera.main != null)
        {
            return Camera.main.transform.position;
        }
        return Vector3.zero;
    }

    private bool CheckLineOfSight(ZombieController zombie, Vector3 targetPos)
    {
        Vector3 startPos = zombie.transform.position + Vector3.up * 1.6f;
        Vector3 direction = (targetPos - startPos).normalized;
        float distance = Vector3.Distance(startPos, targetPos);

        if (Physics.SphereCast(startPos, visionSphereRadius, direction, out RaycastHit hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.IsChildOf(zombie.transform))
            {
                return false;
            }

            if (hit.transform == zombie.target ||
                hit.transform.IsChildOf(zombie.target) ||
                hit.transform.GetComponentInParent<IPlayerSensor>() != null)
            {
                return true;
            }
        }
        return false;
    }

    private void SetRandomDestination(ZombieController zombie)
    {
        walkTimer = 0f;
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += zombie.homePosition;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
        {
            NavMeshAgent agent = zombie.GetComponent<NavMeshAgent>();
            if (agent != null && agent.isOnNavMesh)
            {
                Vector3 safePoint = GetSafeDestination(hit.position);
                agent.SetDestination(safePoint);
            }
        }
    }

    private void SetNextMoanTime()
    {
        moanTimer = 0f;
        nextMoanTime = Random.Range(7.0f, 13.0f);
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

    private void PlayRandomMoan(ZombieController zombie)
    {
        if (zombie.voiceAudioSource == null ||
            zombie.idleMoanClips == null ||
            zombie.idleMoanClips.Length == 0)
            return;

        int index;

        do
        {
            index = Random.Range(0, zombie.idleMoanClips.Length);
        }
        while (zombie.idleMoanClips.Length > 1 && index == lastMoanIndex);

        lastMoanIndex = index;

        zombie.voiceAudioSource.PlayOneShot(zombie.idleMoanClips[index]);
    }

    private void PlayRandomIdleStandSound(ZombieController zombie)
    {
        if (zombie.voiceAudioSource == null ||
            zombie.idleStandClips == null ||
            zombie.idleStandClips.Length == 0)
            return;

        int index;

        do
        {
            index = Random.Range(0, zombie.idleStandClips.Length);
        }
        while (zombie.idleStandClips.Length > 1 && index == lastStandIndex);

        lastStandIndex = index;

        zombie.voiceAudioSource.PlayOneShot(zombie.idleStandClips[index]);
    }
}