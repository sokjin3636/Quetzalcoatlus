using UnityEngine;

public class ItemPickupRelay : MonoBehaviour
{
    public ShelfNode parentShelf;
    private Rigidbody myRb;

    private void Awake()
    {
        myRb = GetComponent<Rigidbody>();
    }

    // 에디터 환경 내 강제 상호작용 테스트용 컨텍스트 메뉴
    [ContextMenu("테스트: VR 없이 아이템 강제 집기")]
    public void OnItemGrabbed()
    {
        if (myRb != null)
        {
            myRb.isKinematic = false;
        }

        if (parentShelf != null)
        {
            parentShelf.TriggerJumpScare();
        }
    }
}