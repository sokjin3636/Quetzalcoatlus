using UnityEngine;

public class ZombieHeadSensor : MonoBehaviour, IImpactReceiver
{
    private ZombieController zombieController;

    [Header("피격 판정 설정")]
    [Tooltip("기절 판정에 요구되는 최소 충격량 임계치")]
    public float minImpactForce = 5.0f;

    void Awake()
    {
        zombieController = GetComponentInParent<ZombieController>();
    }

    public void ReceiveImpact(float force, Vector3 hitPoint)
    {
        Debug.Log($"[좀비 머리] 타격 감지! 전달받은 힘: {force:F2}");

        if (force >= minImpactForce)
        {
            Debug.Log("<color=green>강한 타격 성공! 좀비가 기절합니다.</color>");

            // 타격 성공 시 컨트롤러를 통해 ZombieKnockdownState로 상태 전이
            if (zombieController != null)
            {
                zombieController.OnHeadHit();
            }
        }
        else
        {
            Debug.Log("<color=red>타격이 너무 약합니다! 기절하지 않습니다.</color>");
        }
    }
}