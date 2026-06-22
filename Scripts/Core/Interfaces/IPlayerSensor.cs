public interface IPlayerSensor
{
    bool IsMoving { get; }

    // 긴장도 비율 (0.0: 안전 ~ 1.0: 패닉)
    float TensionRatio { get; }
}