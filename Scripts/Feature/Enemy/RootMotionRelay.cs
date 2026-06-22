using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Animator))]
public class RootMotionRelay : MonoBehaviour
{
    private Animator anim;
    private Transform parentTransform;
    private NavMeshAgent parentAgent;

    void Awake()
    {
        anim = GetComponent<Animator>();
        parentTransform = transform.parent;

        if (parentTransform != null)
        {
            parentAgent = parentTransform.GetComponent<NavMeshAgent>();
        }
    }

    void OnAnimatorMove()
    {
        if (anim == null || parentTransform == null) return;

        // 델타 포지션 강제 적용을 통한 루트 모션 동기화
        parentTransform.position += anim.deltaPosition;

        if (parentAgent != null)
        {
            parentAgent.nextPosition = parentTransform.position;
        }
    }
}