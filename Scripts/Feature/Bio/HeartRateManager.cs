using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class HeartRateManager : MonoBehaviour
{
    [Header("실시간 심박계 데이터")]
    public int currentBPM;
    public float currentRR;
    public float currentRMSSD;

    private List<float> rrList = new List<float>();
    private const int MaxRRCount = 30;

    private bool isActive = false;

    // 캘리브레이션 측정 시작 콜백
    public void StartDataCollection()
    {
        isActive = true;
        rrList.Clear();
        currentRMSSD = 0f;
        Debug.Log("[HeartRateManager] 데이터 수집 시작");
    }

    void Update()
    {
        // 에디터 테스트용 더미 패킷 발생
        if (Input.GetKeyDown(KeyCode.Space))
        {
            byte[] testPacket = { 0x16, 0x4C, 0x2C, 0x03, 0x2B, 0x03 };
            ParseHeartRateData(testPacket);
        }
    }

    // BLE 패킷 파싱 로직
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

    private void ProcessRRInterval(float rr)
    {
        rrList.Add(rr);

        if (rrList.Count > MaxRRCount)
        {
            rrList.RemoveAt(0);
        }

        // 윈도우 사이즈(30) 도달 시에만 RMSSD 계산 수행
        if (rrList.Count == MaxRRCount)
        {
            currentRMSSD = CalculateRMSSD(rrList);
        }
        else
        {
            currentRMSSD = 0f;
        }
    }

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
}