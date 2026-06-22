using UnityEngine;
using UnityEngine.AI;

public class ZombieAttackState : IZombieState
{
    private float struggleTimer = 0f;
    private readonly float fatalTime = 5.0f;
    private bool isPlayerDead = false;

    private readonly float attackDistance = 0.6f;

    public void Enter(ZombieController zombie)
    {
        Debug.Log("좀비 공격 컷씬 시작: 5초 카운트다운 돌입");
        struggleTimer = 0f;
        isPlayerDead = false;
        zombie.Movement.Stop();

        zombie.Anim.CrossFade("Attack", 0.1f);

        if (zombie.voiceAudioSource != null && zombie.attackClip != null)
        {
            zombie.voiceAudioSource.PlayOneShot(zombie.attackClip);
        }

        if (zombie.target != null)
        {
            IAttackTarget attackTarget = zombie.target.GetComponent<IAttackTarget>();
            attackTarget?.OnGrabbedByZombie(zombie.transform);
        }
    }

    public void Execute(ZombieController zombie)
    {
        if (isPlayerDead) return;

        struggleTimer += Time.deltaTime;

        if (zombie.target != null)
        {
            Transform headTransform = Camera.main != null ? Camera.main.transform : zombie.target;

            Vector3 targetPos = headTransform.position + (headTransform.forward * attackDistance);
            targetPos.y = zombie.transform.position.y;

            NavMeshAgent agent = zombie.GetComponent<NavMeshAgent>();
            if (agent != null && agent.isOnNavMesh) agent.Warp(targetPos);
            else zombie.transform.position = targetPos;

            Vector3 lookDir = headTransform.position - zombie.transform.position;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                zombie.transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }

        if (struggleTimer >= fatalTime)
        {
            isPlayerDead = true;
            if (zombie.target != null)
            {
                IAttackTarget attackTarget = zombie.target.GetComponent<IAttackTarget>();
                attackTarget?.OnFatalAttack();
            }
        }
    }

    public void Exit(ZombieController zombie)
    {
        if (zombie.target != null && !isPlayerDead)
        {
            IAttackTarget attackTarget = zombie.target.GetComponent<IAttackTarget>();
            attackTarget?.OnReleased();
        }
    }
}