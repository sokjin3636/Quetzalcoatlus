using System;
using UnityEngine;

namespace Quetzalcoatlus.Core.SignalProcessing
{
    // Biquad 기반 대역통과 필터 구현체
    public sealed class BiquadBandpassFilter : ISignalFilter
    {
        // 필터 계수 (초기화 후 고정)
        private readonly float _b0, _b1, _b2;
        private readonly float _a1, _a2;

        // 이전 상태 버퍼
        private float _x1, _x2, _y1, _y2;

        public BiquadBandpassFilter(float sampleRate, float centerFrequency, float bandwidth)
        {
            if (sampleRate <= 0f) throw new ArgumentOutOfRangeException(nameof(sampleRate));

            // 필터 계수 연산
            float angularFrequency = 2f * Mathf.PI * centerFrequency / sampleRate;
            float qFactor = centerFrequency / bandwidth;
            float alpha = Mathf.Sin(angularFrequency) / (2f * qFactor);

            float a0 = 1f + alpha;

            _b0 = alpha / a0;
            _b1 = 0f;
            _b2 = -alpha / a0;
            _a1 = -2f * Mathf.Cos(angularFrequency) / a0;
            _a2 = (1f - alpha) / a0;

            Reset();
        }

        // 입력 데이터 필터링 처리
        public float Process(float input)
        {
            // 차분 방정식 적용
            float output = (_b0 * input) + (_b1 * _x1) + (_b2 * _x2) - (_a1 * _y1) - (_a2 * _y2);

            // 버퍼 업데이트
            _x2 = _x1;
            _x1 = input;
            _y2 = _y1;
            _y1 = output;

            return output;
        }

        public void Reset()
        {
            _x1 = _x2 = _y1 = _y2 = 0f;
        }
    }
}