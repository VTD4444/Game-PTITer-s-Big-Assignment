using UnityEngine;
using UnityEngine.EventSystems;

public enum SlotType { Basket, Pot } // Giỏ hàng (Tủ lạnh) hoặc Nồi (Bếp)

public class CookingSlot : MonoBehaviour, IDropHandler
{
    public SlotType type;

    public void OnDrop(PointerEventData eventData)
    {
        // Lấy object đang được kéo thả
        GameObject droppedObj = eventData.pointerDrag;
        
        // Vì logic Infinite Source tạo ra object mới, ta cần lấy component từ object đang bay
        CookingDraggable draggable = null;
        
        // Kiểm tra xem người chơi đang kéo cái gốc hay cái bản sao (dragObject)
        // Trong hệ thống EventSystem, pointerDrag chính là cái object đang di chuyển
        draggable = droppedObj.GetComponent<CookingDraggable>();

        if (draggable != null)
        {
            if (type == SlotType.Basket)
            {
                // Thêm vào giỏ hàng
                CookingManager.Instance.AddToBasket(draggable.ingredientType);
            }
            else if (type == SlotType.Pot)
            {
                // Thêm vào nồi
                CookingManager.Instance.AddToPot(draggable.ingredientType);
                // Sau khi thêm vào nồi thì xóa icon trên tay đi
                Destroy(droppedObj); 
            }
        }
    }
}