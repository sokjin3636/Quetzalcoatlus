using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class BackpackTrigger : MonoBehaviour
{
    [Header("--- 오디오 설정 ---")]
    // 인스펙터에서 할당된 인벤토리 오디오 소스
    public AudioSource backpackAudioSource;

    private void OnTriggerEnter(Collider other)
    {
        // 1. 그랩 중인 아이템이 가방 영역에 진입 시 진입 사운드 재생
        if (other.CompareTag("Grabbable"))
        {
            XRGrabInteractable item = other.GetComponent<XRGrabInteractable>();

            if (item != null && item.isSelected)
            {
                PlaySound();
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Grabbable"))
        {
            XRGrabInteractable item = other.GetComponent<XRGrabInteractable>();

            if (item != null)
            {
                // 2. 가방 영역 내에서 그랩을 해제(트리거 릴리스)할 경우 수납 처리
                if (!item.isSelected)
                {
                    StoreInBackpack(other.gameObject);
                }
            }
        }
    }

    private void StoreInBackpack(GameObject item)
    {
        // 인벤토리 데이터 매니저에 아이템 등록
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(item);
        }

        // 수납 완료 피드백 사운드 재생
        PlaySound();

        Debug.Log(item.name + "이(가) 가방에 수납되었습니다.");
    }

    // 오디오 소스의 기본 클립 단발성 재생
    private void PlaySound()
    {
        if (backpackAudioSource != null)
        {
            backpackAudioSource.PlayOneShot(backpackAudioSource.clip);
        }
    }
}