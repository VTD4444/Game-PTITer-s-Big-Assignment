using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BaristaDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public BaristaIngredient type;
    
    [HideInInspector] public GameObject dragObject; // Public để Cốc có thể truy cập nếu cần, hoặc dùng hàm dưới
    private Canvas canvas;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Tạo bản sao hình ảnh
        dragObject = Instantiate(gameObject, canvas.transform);
        dragObject.transform.position = transform.position;

        // Xử lý Raycast cho bản sao để xuyên thấu
        CanvasGroup cg = dragObject.GetComponent<CanvasGroup>();
        if (cg == null) cg = dragObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false; 

        // Xóa script logic trên bản sao để tránh lỗi
        Destroy(dragObject.GetComponent<BaristaDraggable>());
        
        // KHÔNG CẦN GẮN TAG NỮA
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragObject != null)
        {
            dragObject.GetComponent<RectTransform>().anchoredPosition += eventData.delta / canvas.scaleFactor;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Nếu thả ra ngoài (Cốc chưa kịp hủy) thì tự hủy
        if (dragObject != null)
        {
            Destroy(dragObject);
            dragObject = null;
        }
    }

    // --- HÀM MỚI: ĐƯỢC GỌI TỪ CỐC KHI THẢ TRÚNG ---
    public void OnDropSuccess()
    {
        if (dragObject != null)
        {
            Destroy(dragObject);
            dragObject = null;
        }
    }
}