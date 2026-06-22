using UnityEngine;

public class ZombieAnimationRelay : MonoBehaviour
{
    private ZombieController zombieController;

    void Awake()
    {
        zombieController = GetComponentInParent<ZombieController>();
    }

    // 걷기 애니메이션 이벤트 연동 함수
    public void PlayFootstepWalkSound()
    {
        if (zombieController != null)
        {
            zombieController.PlayFootstepWalkSound();
        }
    }

    // 달리기 애니메이션 이벤트 연동 함수
    public void PlayFootstepRunSound()
    {
        if (zombieController != null)
        {
            zombieController.PlayFootstepWalkSound();
        }
    }
}