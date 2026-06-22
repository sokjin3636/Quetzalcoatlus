using UnityEngine;

public class JumpScareRandomizer : MonoBehaviour
{
    [Header("Random Settings")]
    [Tooltip("배치된 트리거 배열 중 런타임 시작 시 단일 트리거만 무작위로 활성화됩니다.")]
    public GameObject[] triggerObjects;

    void Start()
    {
        if (triggerObjects == null || triggerObjects.Length == 0) return;

        // 활성화할 트리거의 무작위 인덱스 선정
        int winningIndex = Random.Range(0, triggerObjects.Length);

        // 선정된 객체를 제외한 나머지 트리거 비활성화 처리
        for (int i = 0; i < triggerObjects.Length; i++)
        {
            if (triggerObjects[i] != null)
            {
                bool isWinner = (i == winningIndex);
                triggerObjects[i].SetActive(isWinner);
            }
        }

        Debug.Log($"[점프스케어 세팅 완료] {gameObject.name} 구역은 '{triggerObjects[winningIndex].name}' 연출로 고정되었습니다.");
    }
}