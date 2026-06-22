using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

public class PlayerController : MonoBehaviour, IAttackTarget
{
    [Header("--- 이동 컴포넌트 참조 ---")]
    public SprintMoveProvider sprintMoveProvider;

    [Header("--- 플레이어 체력 및 UI 설정 ---")]
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;

    [Tooltip("Heart_1, Heart_2, Heart_3 오브젝트 리스트")]
    public List<GameObject> heartUIList = new List<GameObject>();

    [Header("--- 컨트롤러 설정 ---")]
    [Tooltip("우측 컨트롤러(RightHand Controller) 오브젝트 할당")]
    public GameObject rightHandController;

    private List<XRBaseInteractor> rightInteractors = new List<XRBaseInteractor>();
    private ActionBasedController rightXRController;

    private List<LocomotionProvider> turnProviders = new List<LocomotionProvider>();
    private bool isDead = false;

    void Start()
    {
        if (sprintMoveProvider == null)
        {
            sprintMoveProvider = GetComponentInChildren<SprintMoveProvider>();
        }

        currentHealth = maxHealth;
        UpdateHeartUI();

        if (rightHandController != null)
        {
            XRBaseInteractor[] interactors = rightHandController.GetComponentsInChildren<XRBaseInteractor>(true);
            rightInteractors.AddRange(interactors);

            rightXRController = rightHandController.GetComponentInChildren<ActionBasedController>();
        }

        // 시야 회전 프로바이더 캐싱 (Snap / Continuous)
        SnapTurnProvider snapTurn = GetComponentInChildren<SnapTurnProvider>();
        if (snapTurn != null) turnProviders.Add(snapTurn);

        ContinuousTurnProvider continuousTurn = GetComponentInChildren<ContinuousTurnProvider>();
        if (continuousTurn != null) turnProviders.Add(continuousTurn);
    }

    // 상호작용 및 시야 회전 제어 (이벤트용)
    public void SetRightHandInteractions(bool enable)
    {
        // 상호작용 컴포넌트 활성화 상태 동기화
        foreach (var interactor in rightInteractors)
        {
            if (interactor != null) interactor.enabled = enable;
        }

        // 컨트롤러 입력 액션 제어
        if (rightXRController != null)
        {
            rightXRController.enableInputActions = enable;
            rightXRController.enabled = enable;
        }

        // 시야 회전(Turn) 컴포넌트 제어
        foreach (var turnProvider in turnProviders)
        {
            if (turnProvider != null)
            {
                turnProvider.enabled = enable;
            }
        }

        Debug.Log($"[PlayerController] 숨참기 이벤트 상태 적용 완료 -> 오른손 조작 및 화면전환 활성화 여부: {enable}");
    }

    // IAttackTarget 인터페이스 구현
    public void OnGrabbedByZombie(Transform zombieTransform)
    {
        if (isDead) return;
        Debug.Log("[Player] 좀비에게 붙잡힘!");
        if (sprintMoveProvider != null) sprintMoveProvider.enabled = false;
        TakeDamage(1);
    }

    public void OnReleased()
    {
        if (isDead) return;
        Debug.Log("[Player] 좀비에게서 풀려남!");
        if (sprintMoveProvider != null) sprintMoveProvider.enabled = true;
    }

    public void OnFatalAttack()
    {
        if (isDead) return;
        Debug.Log("[Player] 5초 종료, 사망!");
        if (sprintMoveProvider != null) sprintMoveProvider.enabled = false;
        TakeDamage(maxHealth);
    }

    private void TakeDamage(int amount)
    {
        if (isDead) return;
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHeartUI();

        if (currentHealth <= 0)
        {
            isDead = true;
            if (GameManager.Instance != null) GameManager.Instance.TriggerGameOver();
        }
    }

    private void UpdateHeartUI()
    {
        for (int i = 0; i < heartUIList.Count; i++)
        {
            if (heartUIList[i] != null) heartUIList[i].SetActive(i < currentHealth);
        }
    }
}