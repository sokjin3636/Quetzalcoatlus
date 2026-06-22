using UnityEngine;
using UnityEngine.AI;

public class ZombieEventState : IZombieState
{
    private string animName;
    private float eventDuration;
    private float timer = 0f;

    public ZombieEventState(string customAnimName, float customDuration)
    {
        animName = customAnimName;
        eventDuration = customDuration;
    }

    public void Enter(ZombieController zombie)
    {
        zombie.Movement.Stop();

        if (zombie.TryGetComponent<NavMeshAgent>(out var agent))
        {
            agent.enabled = false;
        }

        zombie.Anim.applyRootMotion = true;

        if (!string.IsNullOrEmpty(animName))
        {
            zombie.Anim.Play(animName, 0, 0f);
        }

        InGameTensionSystem tensionSystem = Object.FindFirstObjectByType<InGameTensionSystem>();
        if (tensionSystem != null)
        {
            tensionSystem.ApplyEventShock(100.0f, "점프스케어 등장!");
        }

        timer = 0f;
    }

    public void Execute(ZombieController zombie)
    {
        timer += Time.deltaTime;

        // 지정된 이벤트 연출 시간(Duration) 동안 상태 유지
        if (timer >= eventDuration)
        {
            RestoreNavMesh(zombie);
            zombie.ChangeState(new ZombieListenState());
        }
    }

    public void Exit(ZombieController zombie)
    {
        if (zombie.Anim != null)
        {
            zombie.Anim.applyRootMotion = false;
        }
    }

    private void RestoreNavMesh(ZombieController zombie)
    {
        if (zombie.TryGetComponent<NavMeshAgent>(out var navAgent))
        {
            navAgent.enabled = true;

            if (NavMesh.SamplePosition(zombie.transform.position, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                navAgent.Warp(hit.position);
            }

            if (navAgent.isOnNavMesh)
            {
                navAgent.isStopped = false;
                navAgent.ResetPath();
            }
            else
            {
                Debug.LogWarning($"[{zombie.gameObject.name}] ZombieEventState: Warp 보정 후에도 에이전트가 네비메쉬 위에 없습니다.");
            }
        }
    }
}