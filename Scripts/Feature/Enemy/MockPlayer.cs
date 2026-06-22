using UnityEngine;

public class MockPlayer : MonoBehaviour, IAttackTarget
{
    [Header("Player Stats")]
    public int lifeStocks = 3;

    [Header("Sensor Test")]
    public bool isMoving = false;
    [Range(0f, 1f)]
    public float micVolume = 0f;

    public bool IsMoving => isMoving;
    public float MicVolume => micVolume;

    public void OnGrabbedByZombie(Transform zombieTransform)
    {
        lifeStocks--;
        Debug.Log($"좀비에게 잡혔습니다! 남은 목숨: {lifeStocks}");

        if (lifeStocks <= 0)
        {
            Debug.Log("목숨이 0이 되었습니다. GAME OVER!");
        }
        else
        {
            Debug.Log("5초 안에 좀비의 머리를 타격하여 탈출해야 합니다.");
        }
    }

    public void OnReleased()
    {
        Debug.Log("좀비에게서 성공적으로 벗어났습니다.");
    }

    public void OnFatalAttack()
    {
        lifeStocks = 0;
        Debug.Log("탈출 실패: 모든 목숨 소모. GAME OVER!");
    }
}