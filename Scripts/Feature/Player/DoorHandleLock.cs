using UnityEngine;

public class DoorHandleLock : MonoBehaviour
{
    public HingeJoint doorHinge;

    [Header("Handle Hinges")]
    public HingeJoint insideHandleHinge;
    public HingeJoint outsideHandleHinge;

    public float unlockAngle = 45f;

    [Header("Audio Settings")]
    public AudioSource doorCreakAudio;
    public float speedThreshold = 0.05f;

    private Rigidbody doorRigidbody;
    public bool isUnlatched = false;

    private bool isBroken = false;

    void Start()
    {
        if (doorHinge != null)
        {
            doorRigidbody = doorHinge.GetComponent<Rigidbody>();
        }
    }

    public void BreakLock()
    {
        isBroken = true;
        UnlockDoor();
        Debug.Log("문의 잠금장치가 파손되어 상시 개방 상태가 되었습니다.");
    }

    void Update()
    {
        if (!isBroken)
        {
            // 양측 손잡이 회전 각도 판별
            float currentInsideAngle = Mathf.Abs(insideHandleHinge.angle);
            float currentOutsideAngle = Mathf.Abs(outsideHandleHinge.angle);

            if (currentInsideAngle >= unlockAngle || currentOutsideAngle >= unlockAngle)
            {
                isUnlatched = true;
            }

            if (isUnlatched)
            {
                UnlockDoor();

                // 손잡이 복귀 및 문 닫힘 상태일 경우 잠금 상태로 복구
                float currentDoorAngle = Mathf.Abs(doorHinge.angle);
                if (currentDoorAngle < 1f && currentInsideAngle < unlockAngle && currentOutsideAngle < unlockAngle)
                {
                    isUnlatched = false;
                }
            }
            else
            {
                LockDoor();
            }
        }

        HandleDoorSound();
    }

    void HandleDoorSound()
    {
        if (doorRigidbody == null || doorCreakAudio == null) return;

        // 잠금 상태 시 물리적 떨림(Jitter) 사운드 재생 방지
        if (!isBroken && !isUnlatched)
        {
            if (doorCreakAudio.isPlaying)
            {
                doorCreakAudio.Stop();
            }
            return;
        }

        float currentRotationSpeed = doorRigidbody.angularVelocity.magnitude;

        if (currentRotationSpeed > speedThreshold)
        {
            if (!doorCreakAudio.isPlaying)
            {
                doorCreakAudio.Play();
            }
            doorCreakAudio.volume = Mathf.Min(currentRotationSpeed * 0.5f, 1f);
        }
        else
        {
            if (doorCreakAudio.isPlaying)
            {
                doorCreakAudio.Stop();
            }
        }
    }

    void UnlockDoor()
    {
        JointLimits limits = doorHinge.limits;
        limits.min = -90f;
        limits.max = 90f;
        doorHinge.limits = limits;
    }

    void LockDoor()
    {
        JointLimits limits = doorHinge.limits;
        limits.min = 0f;
        limits.max = 0f;
        doorHinge.limits = limits;
    }
}