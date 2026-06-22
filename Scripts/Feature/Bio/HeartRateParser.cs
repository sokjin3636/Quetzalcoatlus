using UnityEngine;
using System;
using System.Collections.Generic;

public class HeartRateParser : MonoBehaviour
{
    public void ParseHeartRateData(byte[] data)
    {
        if (data == null || data.Length < 2) return;

        // 1. Flags 바이트 확인
        byte flags = data[0];

        // 2. 심박수(BPM) 데이터
        int bpm = data[1];
        Debug.Log($"[BPM] 현재 심박수: {bpm}");

        // 3. R-R Interval 데이터 추출
        // 플래그의 4번째 비트(0x10)가 1일 경우 R-R 데이터가 포함됨
        bool hasRR = (flags & 0x10) != 0;

        if (hasRR)
        {
            // 세 번째 바이트부터 2바이트 단위로 파싱 (Little Endian)
            for (int i = 2; i + 1 < data.Length; i += 2)
            {
                ushort rawRR = BitConverter.ToUInt16(data, i);

                // 단위 변환: 1/1024초 -> 밀리초(ms)
                float rrMs = rawRR * (1000f / 1024f);

                Debug.Log($"[R-R] 검출된 간격: {rrMs:F1} ms");

                AddToSDNNCalculation(rrMs);
            }
        }
    }

    private void AddToSDNNCalculation(float newRR)
    {
        // TODO: SDNN 연산 로직 추가 필요
    }
}