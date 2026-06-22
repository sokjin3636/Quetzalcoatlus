public interface ITensionProvider
{
    // 캘리브레이션 준비 및 변수 초기화
    void StartCalibration();

    // 캘리브레이션 구간 데이터 누적 (매 프레임 호출)
    void CollectCalibrationData();

    // 캘리브레이션 종료 및 최종 기준점(Baseline) 산출
    void FinishCalibration();

    // 임계치 초과에 따른 스트레스 점수 산출 및 반환
    float GetRawStressScore();

    // 이벤트(비명 감지 등) 발생 시 즉각 추가 점수 반환
    float GetInstantAddition();

    // 현재 상태 변동을 유발한 원인 메시지 반환
    string GetActiveReason();
}