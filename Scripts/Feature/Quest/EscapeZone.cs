using UnityEngine;

public class EscapeZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 플레이어 진입 판정 및 퀘스트 완료 여부 검사
        if (other.CompareTag("Player"))
        {
            if (QuestManager.Instance != null && QuestManager.Instance.IsQuestComplete())
            {
                Debug.Log("모든 퀘스트 물품 확보 완료! 탈출 성공!");
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TriggerGameClear();
                }
            }
            else
            {
                Debug.Log("아직 모으지 못한 물품이 있습니다. 마트를 더 수색하십시오.");
            }
        }
    }
}