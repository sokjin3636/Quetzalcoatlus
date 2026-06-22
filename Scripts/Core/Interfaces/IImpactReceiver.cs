using UnityEngine;

public interface IImpactReceiver
{
    // 피격 정보 (충격량, 타격 위치) 전달용 인터페이스
    void ReceiveImpact(float force, Vector3 hitPoint);
}