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
            // Tủ lạnh: Tạo ra một icon tạm thời để kéo đi
            dragObject = Instantiate(gameObject, canvas.transform);
            dragObject.GetComponent<CanvasGroup>().blocksRaycasts = false;
            // Xóa script drag ở bản sao để tránh lỗi đệ quy logic
            Destroy(dragObject.GetComponent<CookingDraggable>());
            // Gắn tạm một tag hoặc component để Slot nhận diện
            var temp = dragObject.AddComponent<CookingDraggable>();
            temp.ingredientType = this.ingredientType;
            temp.isInfiniteSource = false; 
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