using UnityEngine;
using UnityEngine.AI;

public class ZombieApproachState : IZombieState
{
    private Vector3 lastKnownPosition;

    private float attackDistance = 1.0f;

    private float loseSightTimer = 0f;
    private float maxLoseSightTime = 2.0f;

    private float losCheckTimer = 0f;
    private float losCheckInterval = 0.1f;
    private bool lastLineOfSightResult = true;

    private readonly RaycastHit[] losHits = new RaycastHit[20];

    public void Enter(ZombieController zombie)
    {
        if (zombie.TryGetComponent(out NavMeshAgent agent))
        {
            agent.speed = 1.8f * DataManager.ZombieSpecMultiplier;
            agent.isStopped = false;
            agent.updateRotation = true;
        }

        if (zombie.target != null)
        {
            lastKnownPosition = zombie.target.position;
        }

        loseSightTimer = 0f;
        losCheckTimer = 0f;
        lastLineOfSightResult = CheckLineOfSight(zombie);

        zombie.Anim.CrossFade("Movement", 0.2f);

        if (zombie.voiceAudioSource != null && zombie.alertScreamClip != null)
        {
            zombie.voiceAudioSource.PlayOneShot(zombie.alertScreamClip);
        }
    }

    public void Execute(ZombieController zombie)
    {
        if (zombie.target == null)
        {
            zombie.ChangeState(new ZombiePatrolState());
            return;
        }

        if (!zombie.TryGetComponent(out NavMeshAgent agent)) return;

        Transform actualPlayerTransform = Camera.main != null ? Camera.main.transform : zombie.target;

        // X, Z 축 기반의 수평 평면 거리 연산 
        Vector3 zombiePosFlat = new Vector3(zombie.transform.position.x, 0, zombie.transform.position.z);
        Vector3 playerPosFlat = new Vector3(actualPlayerTransform.position.x, 0, actualPlayerTransform.position.z);

        float horizontalDistanceToPlayer = Vector3.Distance(zombiePosFlat, playerPosFlat);

        // 공격 사거리 도달 검사
        if (horizontalDistanceToPlayer <= zombie.CurrentAttackRange)
        {
            zombie.ChangeState(new ZombieAttackState());
            return;
        }

        // 추적 포기 거리 이탈 검사
        if (horizontalDistanceToPlayer > zombie.CurrentGiveUpDistance)
        {
            zombie.ChangeState(new ZombieInvestigateState(actualPlayerTransform.position));
            return;
        }

        // 시야 확보 상태 갱신
        losCheckTimer += Time.deltaTime;
        if (losCheckTimer >= losCheckInterval)
        {
            losCheckTimer = 0f;
            lastLineOfSightResult = CheckLineOfSight(zombie);
        }

        // 플레이어의 현재 위치로 경로 갱신
        agent.SetDestination(actualPlayerTransform.position);

        // 시야 상실 지속 시간 판정
        if (lastLineOfSightResult)
        {
            loseSightTimer = 0f;
        }
        else
        {
            loseSightTimer += Time.deltaTime;

            if (loseSightTimer >= maxLoseSightTime)
            {
                zombie.ChangeState(new ZombieInvestigateState(actualPlayerTransform.position));
                return;
            }
        }
    }

    public void Exit(ZombieController zombie)
    {
    }

    private bool CheckLineOfSight(ZombieController zombie)
    {
        Transform headTransform = Camera.main != null ? Camera.main.transform : zombie.target;

        Vector3 startPos = zombie.transform.position + Vector3.up * 1.5f + zombie.transform.forward * 0.5f;
        Vector3 targetPos = headTransform.position;

        Vector3 direction = (targetPos - startPos).normalized;
        float distance = Vector3.Distance(startPos, targetPos);

        int hitCount = Physics.RaycastNonAlloc(startPos, direction, losHits, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount - 1; i++)
        {
            for (int j = i + 1; j < hitCount; j++)
            {
                if (losHits[i].distance > losHits[j].distance)
                {
                    RaycastHit temp = losHits[i];
                    losHits[i] = losHits[j];
                    losHits[j] = temp;
                }
            }
        }

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = losHits[i];

            if (hit.transform.GetComponentInParent<ZombieController>() != null) continue;

            // 플레이어 또는 센서 객체 피격 시 시야 확보 판정
            if (hit.transform == zombie.target || hit.transform.IsChildOf(zombie.target) || hit.transform.GetComponentInParent<IPlayerSensor>() != null)
            {
                return true;
            }

            return false;
        }

        // 레이캐스트가 장애물에 충돌하지 않고 도달한 경우 (콜라이더 누락 예외 처리)
        return true;
    }
}