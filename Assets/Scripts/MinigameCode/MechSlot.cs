using UnityEngine;
using UnityEngine.EventSystems;
using Photon.Pun;

public class MechSlot : MonoBehaviour, IDropHandler
{
    public int machineID; // 0-3 (Máy nào)
    public int slotID;    // 0-3 (Khe nào trong máy)
    public bool isInventory; // Có phải là kho chứa không?

    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObj = eventData.pointerDrag;
        MechDraggable core = droppedObj.GetComponent<MechDraggable>();

        if (core != null)
        {
            // Thay vì tự set parent, ta gửi yêu cầu lên Server:
            // "Tôi muốn chuyển Core có ID X vào Slot Y của Máy Z"
            MinigameMechanical.Instance.RequestMoveCore(core.coreID, machineID, slotID, isInventory);
        }
    }
}