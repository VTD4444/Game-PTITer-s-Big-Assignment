using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CookingDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Data")]
    public Ingredient ingredientType;
    public bool isInfiniteSource; // Nếu true (ở Tủ lạnh): Kéo đi không mất, tạo bản sao.
                                  // Nếu false (ở Túi/Bếp): Kéo đi là mất (chuyển vị trí).

    [HideInInspector] public Transform parentAfterDrag;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas canvas;
    private GameObject dragObject; // Vật thể thực sự được kéo (dùng cho Infinite Source)

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isInfiniteSource)
        {
            // --- CÁCH MỚI: TẠO OBJECT SẠCH (CLEAN) ---
        
            // 1. Tạo một GameObject rỗng mới tên là "DragIcon"
            dragObject = new GameObject("DragIcon");
        
            // 2. Gán nó làm con của Canvas ngay lập tức
            if (canvas == null) canvas = GetComponentInParent<Canvas>().rootCanvas;
            dragObject.transform.SetParent(canvas.transform, false);
        
            // 3. Thêm component Image và copy Sprite từ icon gốc sang
            Image newImage = dragObject.AddComponent<Image>();
            Image sourceImage = GetComponent<Image>();
            newImage.sprite = sourceImage.sprite;
            newImage.color = sourceImage.color;
            newImage.preserveAspect = sourceImage.preserveAspect; // Giữ tỉ lệ ảnh
        
            // [FIX QUAN TRỌNG] Tắt Raycast Target để chuột xuyên qua được (quan trọng cho OnDrop)
            newImage.raycastTarget = false; 

            // 4. Copy kích thước chính xác
            RectTransform sourceRect = GetComponent<RectTransform>();
            RectTransform newRect = dragObject.GetComponent<RectTransform>();
            newRect.sizeDelta = sourceRect.rect.size;
        
            // 5. Đặt vị trí trùng với icon gốc (dùng World Position)
            dragObject.transform.position = transform.position;

            // 6. Thêm CanvasGroup (để xử lý tương tác nếu cần, hoặc để giống logic cũ)
            CanvasGroup cg = dragObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false; 
            cg.alpha = 0.6f; // [Mẹo] Làm mờ đi một chút để người chơi biết đang kéo (tùy chọn)

            // 7. Gán script logic vào để Slot nhận diện
            var temp = dragObject.AddComponent<CookingDraggable>();
            temp.ingredientType = this.ingredientType;
            temp.isInfiniteSource = false; 
        
            // Đưa lên lớp trên cùng hiển thị
            dragObject.transform.SetAsLastSibling();
        }
        else
        {
            // Bếp/Giỏ: Kéo chính nó
            dragObject = gameObject;
            parentAfterDrag = transform.parent;
            transform.SetParent(canvas.transform);
            transform.SetAsLastSibling();
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragObject != null)
        {
            Debug.Log(dragObject.name);
            dragObject.GetComponent<RectTransform>().anchoredPosition += eventData.delta / canvas.scaleFactor;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isInfiniteSource)
        {
            // Nếu là nguồn vô tận, thả ra mà không vào Slot nào thì hủy bản sao
            Destroy(dragObject);
        }
        else
        {
            // Nếu là vật phẩm thường, quay về chỗ cũ nếu không ai nhận
            dragObject.transform.SetParent(parentAfterDrag);
            canvasGroup.blocksRaycasts = true;
            dragObject.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            Destroy(dragObject);
        }
        dragObject = null;
    }
}