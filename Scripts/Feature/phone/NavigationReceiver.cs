using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NavigationReceiver : MonoBehaviour
{
    [Header("Navigation")]
    public NavigationGraphLoader graphLoader;
    public PathController pathController;

    [Header("Buttons")]
    public Button buttonB;
    public Button buttonC;
    public Button buttonD;

    [Header("Button Texts")]
    public TMP_Text buttonBText;
    public TMP_Text buttonCText;
    public TMP_Text buttonDText;

    [Header("Assign Settings")]
    public bool resetOnStart = true;

    private int assignedCount = 0;

    private string itemB = "";
    private string itemC = "";
    private string itemD = "";

    public Color normalButtonColor = Color.white;

    private void Start()
    {
        assignedCount = 0;
    }

    private void OnEnable()
    {
        GameEventManager.OnItemSpawnedInZone += UpdateNavigationTarget;
    }

    private void OnDisable()
    {
        GameEventManager.OnItemSpawnedInZone -= UpdateNavigationTarget;
    }

    // 아이템 스폰 이벤트를 수신하여 내비게이션 목적지 노드 동적 할당
    private void UpdateNavigationTarget(Vector3 itemPosition, string itemName)
    {
        if (graphLoader == null || pathController == null)
        {
            Debug.LogWarning("NavigationReceiver: graphLoader 또는 pathController가 비어 있습니다.");
            return;
        }

        string nearestNodeId = graphLoader.GetNearestNodeId(itemPosition);

        if (string.IsNullOrEmpty(nearestNodeId))
        {
            Debug.LogWarning($"NavigationReceiver: 가까운 대표노드를 찾지 못했습니다. 위치: {itemPosition}, 아이템: {itemName}");
            return;
        }

        if (assignedCount == 0)
        {
            pathController.destinationB = nearestNodeId;
            itemB = itemName;

            if (buttonBText != null) buttonBText.text = itemName;
            if (buttonB != null) buttonB.interactable = true;

            Debug.Log($"B 버튼 목적지 배정 완료 - 노드: {nearestNodeId}, 아이템 이름: {itemName}");
        }
        else if (assignedCount == 1)
        {
            pathController.destinationC = nearestNodeId;
            itemC = itemName;

            if (buttonCText != null) buttonCText.text = itemName;
            if (buttonC != null) buttonC.interactable = true;

            Debug.Log($"C 버튼 목적지 배정 완료 - 노드: {nearestNodeId}, 아이템 이름: {itemName}");
        }
        else if (assignedCount == 2)
        {
            pathController.destinationD = nearestNodeId;
            itemD = itemName;

            if (buttonDText != null) buttonDText.text = itemName;
            if (buttonD != null) buttonD.interactable = true;

            Debug.Log($"D 버튼 목적지 배정 완료 - 노드: {nearestNodeId}, 아이템 이름: {itemName}");
        }
        else
        {
            Debug.Log("NavigationReceiver: B/C/D 목적지가 이미 모두 배정되었습니다.");
            return;
        }

        assignedCount++;
    }

    // 아이템 획득 시 연관된 내비게이션 버튼 비활성화 처리
    public void DisableButtonForItem(string itemName)
    {
        itemName = CleanItemName(itemName);

        if (!string.IsNullOrEmpty(itemB) && CleanItemName(itemB) == itemName)
        {
            if (buttonB != null) buttonB.interactable = false;
            Debug.Log($"B 버튼 비활성화: {itemName}");
        }

        if (!string.IsNullOrEmpty(itemC) && CleanItemName(itemC) == itemName)
        {
            if (buttonC != null) buttonC.interactable = false;
            Debug.Log($"C 버튼 비활성화: {itemName}");
        }

        if (!string.IsNullOrEmpty(itemD) && CleanItemName(itemD) == itemName)
        {
            if (buttonD != null) buttonD.interactable = false;
            Debug.Log($"D 버튼 비활성화: {itemName}");
        }

        if (pathController != null)
        {
            pathController.ClearRoute();
            pathController.ClearSelectedButton();
        }
    }

    public void ResetAssignedDestinations()
    {
        assignedCount = 0;

        itemB = "";
        itemC = "";
        itemD = "";

        if (buttonBText != null) buttonBText.text = "B";
        if (buttonCText != null) buttonCText.text = "C";
        if (buttonDText != null) buttonDText.text = "D";

        if (buttonB != null) buttonB.interactable = true;
        if (buttonC != null) buttonC.interactable = true;
        if (buttonD != null) buttonD.interactable = true;

        Debug.Log("NavigationReceiver: 목적지 배정 초기화");
    }

    private string CleanItemName(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Replace("(Clone)", "").Trim();
    }
}