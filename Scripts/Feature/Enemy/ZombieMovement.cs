using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class ZombieMovement : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator anim;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();

        agent.updatePosition = true;
        if (anim != null) anim.applyRootMotion = false;
    }

    void Update()
    {
        float currentSpeed = 0f;

        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            // 이동 속도는 상태(State) 컨트롤러에서 전담하며, 여기서는 애니메이션 파라미터만 동기화
            if (!agent.isStopped)
            {
                currentSpeed = agent.velocity.magnitude;
            }
        }

        anim.SetFloat(SpeedHash, currentSpeed);
    }

    public void MoveTo(Vector3 destination)
    {
        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(destination);
        }
    }

    public void Stop()
    {
        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.SetDestination(transform.position);
        }
    }
}