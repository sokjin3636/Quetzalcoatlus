using System.Collections;
using UnityEngine;

public class DoorKicker : MonoBehaviour
{
    [Header("문 걷어차기 물리 설정")]
    public Rigidbody doorRigidbody;
    public float kickForce = 14f;
    public Vector3 kickTorqueAxis = Vector3.up;

    [Header("역방향 보정 설정")]
    [Tooltip("역방향(-1) 타격 시 물리적 마찰 상쇄를 위한 가중치 배수")]
    public float backwardForceMultiplier = 1.5f;

    [Header("열림 고정 설정")]
    public float lockTime = 0.4f;
    public float lockAngularDrag = 100f;

    public void KickDoor(float directionMultiplier)
    {
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.enabled = false;
        }

        // 스크립트 비활성화 대신 잠금 상태만 강제 해제 처리
        DoorHandleLock handleLock = GetComponent<DoorHandleLock>();
        if (handleLock != null)
        {
            handleLock.BreakLock();
        }

        if (doorRigidbody == null)
        {
            doorRigidbody = GetComponent<Rigidbody>();
        }

        if (doorRigidbody != null)
        {
            HingeJoint hinge = doorRigidbody.GetComponent<HingeJoint>();
            if (hinge != null)
            {
                hinge.useSpring = false;
                hinge.useMotor = false;
            }

            doorRigidbody.isKinematic = false;

            float finalForce = kickForce * directionMultiplier;
            if (directionMultiplier < 0)
            {
                finalForce *= backwardForceMultiplier;
            }

            doorRigidbody.AddRelativeTorque(kickTorqueAxis * finalForce, ForceMode.Impulse);

            StartCoroutine(LockDoorOpen());
        }
    }

    private IEnumerator LockDoorOpen()
    {
        yield return new WaitForSeconds(lockTime);

        if (doorRigidbody != null)
        {
            doorRigidbody.angularVelocity = Vector3.zero;
            doorRigidbody.angularDamping = lockAngularDrag;
        }
    }

    [ContextMenu("Test: Force Kick Door")]
    public void TestKickDoor()
    {
        KickDoor(1f);
    }
}