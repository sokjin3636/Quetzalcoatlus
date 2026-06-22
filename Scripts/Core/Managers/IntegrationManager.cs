using UnityEngine;

public class IntegrationManager : MonoBehaviour
{
    [Header("Core Systems")]
    public InGameTensionSystem tensionSystem;

    // 시야 이펙트 및 심박음 제어 스크립트 연결
    public TensionEffectManager effectManager;
    public SoundTensionProvider soundProvider;

    [Header("Panic Settings")]
    public float panicThreshold = 75f;
    private bool isPanicMode = false;

    void Start()
    {
        // 텐션 상태 방송 채널 구독
        if (tensionSystem != null)
        {
            tensionSystem.OnTensionStateBroadcast += HandleTensionState;
        }
    }

    void OnDestroy()
    {
        // 메모리 누수 방지용 구독 해제
        if (tensionSystem != null)
        {
            tensionSystem.OnTensionStateBroadcast -= HandleTensionState;
        }
    }

    private void HandleTensionState(float tension, string stateDirection)
    {
        // 1. 긴장도 수치에 비례한 이펙트 및 사운드 조절 로직 
        // (EffectManager / SoundProvider의 public 함수 호출부)

        // 2. 패닉 상태 진입 조건: 임계치 이상 & 상승 중일 때
        if (tension >= panicThreshold && stateDirection.Contains("[상승 중]") && !isPanicMode)
        {
            TriggerPanicEvent();
        }
        else if (tension < panicThreshold - 15f && isPanicMode)
        {
            EndPanicEvent();
        }
    }

    private void TriggerPanicEvent()
    {
        isPanicMode = true;
        Debug.Log("[Integration] 긴장도 임계치 돌파. 패닉 연출 활성화.");

        // TODO: 기획에 따라 좀비 스폰, 이속 감소 등 추가 패닉 연출 구현
    }

    private void EndPanicEvent()
    {
        isPanicMode = false;
        Debug.Log("[Integration] 긴장도 완화. 패닉 연출 종료.");
    }
}