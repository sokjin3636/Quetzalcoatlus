using UnityEngine;
using UnityEngine.UI;

public class DynamicDifficultyProvider : MonoBehaviour
{
    public Toggle dynamicDifficultyToggle;

    private void Start()
    {
        // PlayerPrefs 기반 동적 난이도 설정값 초기화 (기본값 1: 활성화)
        int savedDifficulty = PlayerPrefs.GetInt("DynamicDifficulty", 1);
        DataManager.UseDynamicDifficulty = (savedDifficulty == 1);

        if (dynamicDifficultyToggle != null)
        {
            dynamicDifficultyToggle.isOn = DataManager.UseDynamicDifficulty;
            dynamicDifficultyToggle.onValueChanged.AddListener(SetDynamicDifficulty);
        }
    }

    // UI 토글 이벤트를 통한 동적 난이도 상태 변경 및 로컬 저장
    public void SetDynamicDifficulty(bool isON)
    {
        DataManager.UseDynamicDifficulty = isON;
        PlayerPrefs.SetInt("DynamicDifficulty", isON ? 1 : 0);
        Debug.Log("동적 난이도 상태 변경 및 저장: " + isON);
    }

    public bool UseDynamicDifficulty
    {
        get
        {
            return DataManager.UseDynamicDifficulty;
        }
    }
}