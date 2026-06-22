using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(ZombieMovement))]
public class ZombieController : MonoBehaviour
{
    public ZombieMovement Movement { get; private set; }
    public Animator Anim { get; private set; }
    private NavMeshAgent agent;

    [Header("Target Settings")]
    [Tooltip("VR 플레이어 타겟")]
    public Transform target;

    [Header("Zone Settings")]
    [Tooltip("좀비 스폰 초기 위치")]
    public Vector3 homePosition;

    [Header("Optimization Settings")]
    [Tooltip("지정된 거리 초과 시 AI 연산 휴면 상태 돌입")]
    public float aiCullDistance = 40.0f;
    private float sqrAiCullDistance;
    private bool isHibernating = false;

    [Header("Balance Settings (Base)")]
    public float baseSightDistance = 15.0f;
    public float baseHearingDistance = 10.0f;
    [Tooltip("추적 포기 거리")]
    public float baseGiveUpDistance = 20.0f;
    [Tooltip("공격 개시 거리")]
    public float baseAttackRange = 1.5f;

    // 동적 난이도 승수에 따른 실시간 스펙 프로퍼티
    public float CurrentSightDistance => baseSightDistance * DataManager.ZombieSpecMultiplier;
    public float CurrentHearingDistance => baseHearingDistance * DataManager.ZombieSpecMultiplier;
    public float CurrentGiveUpDistance => baseGiveUpDistance * DataManager.ZombieSpecMultiplier;
    public float CurrentAttackRange => baseAttackRange * DataManager.ZombieSpecMultiplier;

    [Header("Audio Sources")]
    public AudioSource voiceAudioSource;
    public AudioSource moveAudioSource;

    [Header("Audio Clips")]
    public AudioClip[] idleMoanClips;
    public AudioClip[] idleStandClips;
    public AudioClip eventGrowlSound;
    public AudioClip alertScreamClip;
    public AudioClip attackClip;
    public AudioClip knockdownClip;
    public AudioClip footstepWalkClip;
    public AudioClip footstepRunClip;
    public AudioClip warningGrowlClip;

    private IZombieState currentState;

    void Awake()
    {
        Movement = GetComponent<ZombieMovement>();
        Anim = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();

        sqrAiCullDistance = aiCullDistance * aiCullDistance;
    }

    void OnEnable()
    {
        AssignPlayerTarget();
        homePosition = transform.position;
        isHibernating = false;

        ChangeState(new ZombiePatrolState());
    }

    void Update()
    {
        if (target == null)
        {
            AssignPlayerTarget();
            return;
        }

        float sqrDistanceToPlayer = (transform.position - target.position).sqrMagnitude;

        if (sqrDistanceToPlayer > sqrAiCullDistance)
        {
            if (!isHibernating)
            {
                SleepAI();
            }
            return;
        }
        else
        {
            if (isHibernating)
            {
                WakeUpAI();
            }
        }

        if (currentState != null)
        {
            currentState.Execute(this);
        }
    }

    public void ChangeState(IZombieState newState)
    {
        currentState?.Exit(this);
        currentState = newState;
        currentState?.Enter(this);
    }

    private void AssignPlayerTarget()
    {
        if (Camera.main != null)
        {
            target = Camera.main.transform.root;
        }
    }

    public void OnHeadHit()
    {
        if (currentState is ZombieAttackState)
        {
            ChangeState(new ZombieKnockdownState());
        }
    }

    public void PlayFootstepWalkSound()
    {
        if (moveAudioSource != null && footstepWalkClip != null)
        {
            moveAudioSource.PlayOneShot(footstepWalkClip);
        }
    }

    public void PlayFootstepRunSound()
    {
        if (moveAudioSource != null && footstepRunClip != null)
        {
            moveAudioSource.PlayOneShot(footstepRunClip);
        }
    }

    private void SleepAI()
    {
        isHibernating = true;

        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        if (Anim != null)
        {
            Anim.enabled = false;
        }
    }

    private void WakeUpAI()
    {
        isHibernating = false;

        if (agent != null)
        {
            agent.enabled = true;

            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
            }
        }

        if (Anim != null)
        {
            Anim.enabled = true;
        }
    }
}