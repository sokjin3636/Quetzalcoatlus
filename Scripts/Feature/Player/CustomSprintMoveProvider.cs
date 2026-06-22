using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

[AddComponentMenu("XR/Locomotion/Custom Sprint Controller", 12)]
public class CustomSprintController : MonoBehaviour
{
    [Header("--- 참조 설정 ---")]
    [Tooltip("로코모션 처리를 담당하는 ContinuousMoveProvider 컴포넌트")]
    public ContinuousMoveProvider moveProvider;

    [Header("--- 달리기 설정 ---")]
    [Tooltip("스프린트 입력 활성화 시 적용될 이동 속도")]
    [SerializeField] float m_SprintSpeed = 3.5f;

    [Tooltip("우측 컨트롤러 스프린트 입력 액션 리더")]
    [SerializeField] XRInputValueReader<bool> m_SprintButtonInput = new XRInputValueReader<bool>("Right Hand Sprint");

    private float m_DefaultNormalSpeed;
    private bool m_IsSpeedInitialized = false;

    void Start()
    {
        // 컴포넌트 자동 할당
        if (moveProvider == null)
        {
            moveProvider = GetComponent<ContinuousMoveProvider>();
        }

        if (moveProvider != null)
        {
            // 기본 이동 속도 초기화 및 저장
            m_DefaultNormalSpeed = moveProvider.moveSpeed;
            m_IsSpeedInitialized = true;
        }
        else
        {
            Debug.LogError("[SprintController] ContinuousMoveProvider를 찾을 수 없습니다! 오브젝트를 확인하세요.");
        }
    }

    void OnEnable()
    {
        // 스프린트 입력 액션 활성화
        m_SprintButtonInput.EnableDirectActionIfModeUsed();
    }

    void OnDisable()
    {
        m_SprintButtonInput.DisableDirectActionIfModeUsed();
    }

    void Update()
    {
        if (!m_IsSpeedInitialized || moveProvider == null) return;

        // 스프린트 버튼 입력 상태 확인
        bool isSprintPressed = m_SprintButtonInput.ReadValue();

        // 입력 상태에 따른 이동 속도 갱신
        if (isSprintPressed)
        {
            moveProvider.moveSpeed = m_SprintSpeed;
        }
        else
        {
            moveProvider.moveSpeed = m_DefaultNormalSpeed;
        }
    }
}