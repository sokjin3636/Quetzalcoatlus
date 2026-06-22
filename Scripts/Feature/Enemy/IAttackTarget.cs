using UnityEngine;

public interface IAttackTarget
{
    // 공격 피격 시 호출
    void OnGrabbedByZombie(Transform zombieTransform);

    // 상태 구속 해제 시 호출
    void OnReleased();

    // 치명적 공격 완료 시 호출 (게임 오버 판정)
    void OnFatalAttack();
}