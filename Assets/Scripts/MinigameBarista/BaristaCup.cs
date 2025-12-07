using UnityEngine;
using UnityEngine.EventSystems;

public class BaristaCup : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObj = eventData.pointerDrag; // Đây là NÚT GỐC
        
        // Lấy script BaristaDraggable trực tiếp từ nút gốc
        BaristaDraggable item = droppedObj.GetComponent<BaristaDraggable>();

        if (item != null)
        {
            Debug.Log("Cốc đã nhận: " + item.type);
            
            // 1. Thêm dữ liệu vào logic game
            BaristaManager.Instance.AddIngredient(item.type);
            
            // 2. Thông báo cho script gốc biết là đã thả trúng đích để nó hủy cái ảnh đi
            item.OnDropSuccess();
        }
    }
}