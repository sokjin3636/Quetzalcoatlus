using UnityEngine;
using UnityEngine.UI;

public class PhoneBatteryUI : MonoBehaviour
{
    public PhonePowerController phonePower;
    public Image batteryFill;

    void Update()
    {
        // 전력 컨트롤러의 배터리 잔량(%)을 UI fillAmount로 실시간 렌더링
        if (phonePower == null || batteryFill == null)
            return;

        batteryFill.fillAmount = phonePower.BatteryPercent / 100f;
    }
}