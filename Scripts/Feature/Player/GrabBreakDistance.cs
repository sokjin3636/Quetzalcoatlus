using UnityEngine;

public class GrabBreakDistance : MonoBehaviour
{
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;

    [Header("--- 상호작용 한계 설정 ---")]
    [SerializeField] private float breakDistance = 0.6f; // 상호작용 해제 한계 거리

    void Awake()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
    }

    void Update()
    {
        // 1. 객체 그랩 상태 확인
        if (grabInteractable.isSelected)
        {
            // 2. 인터랙터와 현재 객체 간의 거리 계산
            float distance = Vector3.Distance(
                grabInteractable.interactorsSelecting[0].transform.position,
                transform.position
            );

            // 3. 한계 거리 초과 시 그랩 강제 해제
            if (distance > breakDistance)
            {
                grabInteractable.interactionManager.SelectExit(
                    (UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor)grabInteractable.interactorsSelecting[0],
                    (UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable)grabInteractable
                );
            }
        }
    }
}