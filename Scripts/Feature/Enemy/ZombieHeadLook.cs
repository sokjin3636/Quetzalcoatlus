using UnityEngine;

[RequireComponent(typeof(Animator))]
public class ZombieHeadLook : MonoBehaviour
{
    private Animator anim;
    private Transform lookTarget;

    private float targetWeight = 0f;
    private float currentWeight = 0f;

    public float lookSpeed = 6.0f;

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    public void SetLookTarget(Transform target, float weight)
    {
        lookTarget = target;
        targetWeight = weight;
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (anim == null) return;

        // 목표 가중치 선형 보간 적용
        currentWeight = Mathf.Lerp(currentWeight, targetWeight, Time.deltaTime * lookSpeed);

        if (lookTarget != null)
        {
            // 플레이어 안면부(1.5m) 응시 오프셋 적용
            anim.SetLookAtPosition(lookTarget.position + Vector3.up * 1.5f);
        }

        // IK 가중치 제어 (전체, 몸통, 머리, 눈, 관절 제한)
        anim.SetLookAtWeight(currentWeight, 0.1f, 1.0f, 0.0f, 0.5f);
    }
}