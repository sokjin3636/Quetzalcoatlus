using UnityEngine;

public class PlayerPhysicsPush : MonoBehaviour
{
    [Header("밀기 힘 설정")]
    public float pushPower = 2.0f;

    // CharacterController 충돌 처리
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;

        // Rigidbody 유효성 및 물리 제어 가능 여부 검사
        if (body == null || body.isKinematic) return;

        // 수직 방향(바닥) 충돌 무시
        if (hit.moveDirection.y < -0.3f) return;

        // 도어 잠금장치(DoorHandleLock) 상태 확인
        DoorHandleLock doorLock = body.GetComponent<DoorHandleLock>();
        if (doorLock != null)
        {
            // 잠금 상태인 경우 물리적 밀기 무시
            if (!doorLock.isUnlatched)
            {
                return;
            }
        }

        // 수평 평면 기준 밀기 방향 벡터 산출
        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);

        // 대상 Rigidbody에 속도(Velocity) 적용
        body.linearVelocity = pushDir * pushPower;
    }
}