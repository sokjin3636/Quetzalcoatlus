using UnityEngine;
using UnityEngine.AI;

public class ZombieListenState : IZombieState
{
    private float listenDuration = 5.0f;
    private float timer = 0f;

    private VRMovementSensor sensor;
    private bool hasPlayedWarning = false;

    private float orbitRadius = 1.5f;
    private float orbitTimer = 0f;

    private ZombieHeadLook headLook;

    public void Enter(ZombieController zombie)
    {
        zombie.Anim.CrossFade("Movement", 0.1f);
        timer = 0f;
        hasPlayedWarning = false;

        sensor = Object.FindFirstObjectByType<VRMovementSensor>();
        headLook = zombie.GetComponentInChildren<ZombieHeadLook>();

        NavMeshAgent agent = zombie.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = true;
            agent.speed = 0.5f;
            agent.isStopped = false;
            agent.updateRotation = true;
        }

        if (zombie.voiceAudioSource != null && zombie.eventGrowlSound != null)
        {
            zombie.voiceAudioSource.Stop();
            zombie.voiceAudioSource.clip = zombie.eventGrowlSound;
            zombie.voiceAudioSource.loop = true;
            zombie.voiceAudioSource.Play();
        }

        SetRandomOrbitPoint(zombie);
    }

    public void Execute(ZombieController zombie)
    {
        timer += Time.deltaTime;

        if (sensor != null)
        {
            // 미세 텐션 비율에 따른 헤드 룩(HeadLook) IK 제어
            if (headLook != null)
            {
                if (sensor.TensionRatio > 0.1f)
                {
                    headLook.SetLookTarget(zombie.target, 1.0f);
                }
                else
                {
                    headLook.SetLookTarget(null, 0f);
                }
            }

            if (!hasPlayedWarning && sensor.TensionRatio >= 0.5f)
            {
                hasPlayedWarning = true;
                if (zombie.voiceAudioSource != null && zombie.warningGrowlClip != null)
                {
                    zombie.voiceAudioSource.Stop();
                    zombie.voiceAudioSource.clip = zombie.warningGrowlClip;
                    zombie.voiceAudioSource.loop = false;
                    zombie.voiceAudioSource.Play();
                }
            }

            if (sensor.IsMoving)
            {
                zombie.ChangeState(new ZombieAttackState());
                return;
            }
        }

        // 목적지 도착 시 일정 시간 대기 후 궤도 재설정 배회 로직
        NavMeshAgent agent = zombie.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
            {
                orbitTimer += Time.deltaTime;
                if (orbitTimer > 1.0f)
                {
                    SetRandomOrbitPoint(zombie);
                    orbitTimer = 0f;
                }
            }
        }

        if (timer >= listenDuration)
        {
            zombie.ChangeState(new ZombiePatrolState(2.0f));
        }
    }

    public void Exit(ZombieController zombie)
    {
        if (zombie.voiceAudioSource != null)
        {
            zombie.voiceAudioSource.Stop();
        }

        if (headLook != null)
        {
            headLook.SetLookTarget(null, 0f);
        }
    }

    private void SetRandomOrbitPoint(ZombieController zombie)
    {
        if (zombie.target == null) return;

        Vector2 randomCircle = Random.insideUnitCircle.normalized * orbitRadius;
        Vector3 randomPos = zombie.target.position + new Vector3(randomCircle.x, 0, randomCircle.y);

        if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
        {
            NavMeshAgent agent = zombie.GetComponent<NavMeshAgent>();
            if (agent != null && agent.isOnNavMesh)
            {
                agent.SetDestination(hit.position);
            }
        }
    }
}