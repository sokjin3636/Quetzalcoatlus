using UnityEngine;
using UnityEngine.XR;

public class PlayerMeleeAttack : MonoBehaviour
{
    [Header("--- 설정 ---")]
    public XRNode handNode;
    public float minImpactForce = 1.0f;

    private Vector3 lastPosition;
    private float currentVelocity;

    void Start()
    {
        lastPosition = transform.position;
    }

    void Update()
    {
        // 위치 변화량을 기반으로 한 현재 속도 연산
        float distance = Vector3.Distance(transform.position, lastPosition);
        currentVelocity = distance / Time.deltaTime;

        lastPosition = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 피격 대상의 인터페이스 구현 여부 확인
        IImpactReceiver target = other.GetComponentInParent<IImpactReceiver>();

        if (target != null)
        {
            // 임계 충격량 도달 여부 검사
            if (currentVelocity >= minImpactForce)
            {
                // 충돌 접점 산출
                Vector3 hitPoint = other.ClosestPoint(transform.position);

                // 피격 인터페이스를 통한 데미지 및 충격 정보 전달
                target.ReceiveImpact(currentVelocity, hitPoint);

                // 햅틱 피드백 트리거
                TriggerHapticFeedback();
                Debug.Log($"<color=orange>[타격]</color> 좀비 가격! 힘: {currentVelocity:F2} | 좌표: {hitPoint}");
            }
        }
    }

    // 지정된 XRNode 컨트롤러에 햅틱 임펄스 전송
    private void TriggerHapticFeedback()
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(handNode);
        if (device.isValid)
        {
            device.SendHapticImpulse(0, 0.5f, 0.1f);
        }
    }
}