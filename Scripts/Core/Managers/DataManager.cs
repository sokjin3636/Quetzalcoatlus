using System.Collections.Generic;

public static class DataManager
{
    public static bool UseHeartRate = false;

    // 캘리브레이션 최종 기준값 저장소
    public static float BaseAvgBPM = 75f;
    public static float BaseRMSSD = 40f;
    public static float BaseTremorEnergy = 0.05f;

    // 캘리브레이션 시 수집된 R-R 간격 데이터
    public static List<float> CalibratedRRList = new List<float>();

    // 동적 난이도 조절 시스템 변수
    public static bool UseDynamicDifficulty = true;
    public static float ZombieSpecMultiplier = 1.0f;
}