public interface IZombieState
{
    // 상태 진입 (최초 1회 실행)
    void Enter(ZombieController zombie);

    // 상태 실행 (해당 상태 유지 중 매 프레임 호출)
    void Execute(ZombieController zombie);

    // 상태 종료 (상태 전이 전 1회 실행)
    void Exit(ZombieController zombie);
}