using UnityEngine;
using System;
using System.Collections.Generic;

public class InGameRealHeartRateManager : MonoBehaviour
{
    [Header("--- 실시간 데이터 ---")]
    public int currentBPM;
    public float currentRR;
    public float currentRMSSD;

    private List<float> rrList = new List<float>();
    private const int MaxRRCount = 30;

    private bool isActive = false;

    void Start()
    {
        // DataManager에 저장된 캘리브레이션 R-R 간격 데이터 연동
        if (DataManager.UseHeartRate && DataManager.CalibratedRRList != null && DataManager.CalibratedRRList.Count > 0)
        {
            rrList = new List<float>(DataManager.CalibratedRRList);
            currentBPM = (int)DataManager.BaseAvgBPM;
            currentRMSSD = CalculateRMSSD(rrList);

            isActive = true;
            Debug.Log($"[InGame-HRManager] 캘리브레이션 데이터 연동 완료. (Count: {rrList.Count})");
        }
        else
        {
            isActive = false;
            currentRMSSD = DataManager.BaseRMSSD;
            currentBPM = (int)DataManager.BaseAvgBPM;
        }
    }

#if UNITY_EDITOR
    void Update()
    {
        // 에디터 테스트용 더미 패킷 발생
        if (Input.GetKeyDown(KeyCode.Space))
        {
            byte[] testPacket = { 0x16, 0x4C, 0x2C, 0x03, 0x2B, 0x03 };
            ParseHeartRateData(testPacket);
        }
    }
#endif

    // BLE 패킷 데이터 파싱
    public void ParseHeartRateData(byte[] data)
    {
        if (!isActive || data == null || data.Length < 2) return;

        currentBPM = data[1];
        bool hasRR = (data[0] & 0x10) != 0;

        if (hasRR)
        {
            for (int i = 2; i + 1 < data.Length; i += 2)
            {
                ushort rawRR = BitConverter.ToUInt16(data, i);
                float rrMs = rawRR * (1000f / 1024f);

                currentRR = rrMs;
                ProcessRRInterval(rrMs);
            }
        }
    }

    // 슬라이딩 윈도우 기반 R-R Interval 데이터 처리
    private void ProcessRRInterval(float rr)
    {
        rrList.Add(rr);

        if (rrList.Count > MaxRRCount)
        {
            rrList.RemoveAt(0);
        }

        if (rrList.Count == MaxRRCount)
        {
            currentRMSSD = CalculateRMSSD(rrList);
        }
        else
        {
            currentRMSSD = 0f;
        }
    }

    // RMSSD 계산
    private float CalculateRMSSD(List<float> list)
    {
        if (list.Count < 2) return 0;
        float sumOfSquaredDifferences = 0f;
        for (int i = 0; i < list.Count - 1; i++)
        {
            float diff = list[i + 1] - list[i];
            sumOfSquaredDifferences += diff * diff;
        }
        float meanSquare = sumOfSquaredDifferences / (list.Count - 1);
        return Mathf.Sqrt(meanSquare);
    }

    public List<float> GetCurrentRRList()
    {
        return new List<float>(rrList);
    }
}