using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;

public class InGameTensionSystem : MonoBehaviour
{
    [Header("Realtime Active Settings (Runtime)")]
    public bool isCalibrating = false;
    private float activeSensitivity;
    private float activeBaseDecayRate;
    private float activeWeightRMSSD;
    private float activeWeightTremor;

    [Header("Realtime Tension")]
    [Range(0, 100)] public float currentTension = 0f;
    public string currentReasonMessage = "";

    [Header("--- 심박계 사용 시 파라미터 ---")]
    public float version1_sensitivity = 4f;
    public float version1_baseDecayRate = 5.0f;
    public float version1_weightRMSSD = 0.5f;
    public float version1_weightTremor = 0.5f;

    [Header("--- 심박계 미사용 시 파라미터 ---")]
    public float version2_sensitivity = 5f;
    public float version2_baseDecayRate = 4.0f;
    public float version2_weightRMSSD = 0.0f;
    public float version2_weightTremor = 1.0f;

    [Header("--- Debug: Dynamic Difficulty ---")]
    [SerializeField] private bool debug_useDynamicDifficulty;
    [SerializeField] private float debug_currentZombieMultiplier;

    public event Action<float, string> OnTensionStateBroadcast;

    private ITensionProvider[] tensionProviders;

    private List<string> cachedReasons = new List<string>(10);
    private StringBuilder reasonBuilder = new StringBuilder(100);

    void Start()
    {
        tensionProviders = GetComponents<ITensionProvider>();
        isCalibrating = false;

        // 심박계 사용 여부에 따른 텐션 가중치 적용
        if (DataManager.UseHeartRate)
        {
            activeSensitivity = version1_sensitivity;
            activeBaseDecayRate = version1_baseDecayRate;
            activeWeightRMSSD = version1_weightRMSSD;
            activeWeightTremor = version1_weightTremor;
            Debug.Log($"[TensionSystem] 심박계 가동 셋팅 적용 완료.");
        }
        else
        {
            activeSensitivity = version2_sensitivity;
            activeBaseDecayRate = version2_baseDecayRate;
            activeWeightRMSSD = version2_weightRMSSD;
            activeWeightTremor = version2_weightTremor;
            Debug.Log($"[TensionSystem] 심박계 미가동 셋팅 적용 완료.");
        }
    }

    void Update()
    {
        CalculateTension();
    }

    private void CalculateTension()
    {
        if (tensionProviders == null || tensionProviders.Length == 0) return;

        float continuousDelta = 0f;
        float instantTotal = 0f;

        cachedReasons.Clear();

        foreach (var provider in tensionProviders)
        {
            float rawScore = provider.GetRawStressScore();

            if (provider is InGameHeartRateTensionProvider || provider.GetType().Name.Contains("HeartRate"))
            {
                continuousDelta += rawScore * activeWeightRMSSD;
            }
            else if (provider is InGameTremorSensorManager || provider.GetType().Name.Contains("Tremor"))
            {
                continuousDelta += rawScore * activeWeightTremor;
            }
            else
            {
                continuousDelta += rawScore;
            }

            instantTotal += provider.GetInstantAddition();

            string r = provider.GetActiveReason();
            if (!string.IsNullOrEmpty(r)) cachedReasons.Add(r);
        }

        float changeAmount = (continuousDelta * activeSensitivity) - (activeBaseDecayRate * Time.deltaTime);

        currentTension += (changeAmount * Time.deltaTime * 10f) + instantTotal;
        currentTension = Mathf.Clamp(currentTension, 0f, 100f);

        string direction = changeAmount > 0.01f ? "[상승 중] " : (changeAmount < -0.01f ? "[감소 중] " : "[평온 상태] ");

        reasonBuilder.Clear();
        reasonBuilder.Append(direction);
        for (int i = 0; i < cachedReasons.Count; i++)
        {
            reasonBuilder.Append(cachedReasons[i]);
            if (i < cachedReasons.Count - 1)
            {
                reasonBuilder.Append(", ");
            }
        }

        currentReasonMessage = reasonBuilder.ToString();

        // 전역 동적 난이도 수치 갱신
        if (DataManager.UseDynamicDifficulty)
        {
            DataManager.ZombieSpecMultiplier = (currentTension >= 33f) ? 0.8f : 1.0f;
        }
        else
        {
            DataManager.ZombieSpecMultiplier = 1.0f;
        }

        debug_useDynamicDifficulty = DataManager.UseDynamicDifficulty;
        debug_currentZombieMultiplier = DataManager.ZombieSpecMultiplier;

        OnTensionStateBroadcast?.Invoke(currentTension, direction);
    }

    // 외부 이벤트에 의한 강제 텐션 증가 처리
    public void ApplyEventShock(float shockAmount, string eventName)
    {
        currentTension += shockAmount;
        currentTension = Mathf.Clamp(currentTension, 0f, 100f);
        currentReasonMessage = $"[이벤트 충격] {eventName}";
        OnTensionStateBroadcast?.Invoke(currentTension, currentReasonMessage);
        Debug.Log($"[TensionSystem] 이벤트 긴장도 상승: +{shockAmount} ({eventName})");
    }
}