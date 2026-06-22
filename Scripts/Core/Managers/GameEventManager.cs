using System;
using UnityEngine;

public static class GameEventManager
{
    // 아이템 스폰 이벤트 (Vector3: 위치, string: 이름)
    public static Action<Vector3, string> OnItemSpawnedInZone;

    public static Action OnPlayerDeath;
    public static Action OnEscapeSuccess;

    // 텐션 레벨 변동 이벤트
    public static Action<float> OnTensionLevelChanged;
}