namespace Quetzalcoatlus.Core.SignalProcessing
{
    // 신호 처리 필터 공통 인터페이스
    public interface ISignalFilter
    {
        // 입력 데이터 필터링 수행
        float Process(float input);

        // 내부 버퍼(상태) 초기화
        void Reset();
    }
}