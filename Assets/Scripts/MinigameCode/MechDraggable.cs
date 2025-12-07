using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MechDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int coreID; // ID định danh của viên này (0-15)
    public int level;  // 1, 2, 3
    
    [HideInInspector] public Transform parentAfterDrag;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Canvas canvas;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Lưu cha cũ để nếu thả ra ngoài thì quay về
        parentAfterDrag = transform.parent;
        transform.SetParent(canvas.transform); // Đưa lên lớp cao nhất để không bị che
        canvasGroup.blocksRaycasts = false;    // Để tia chuột xuyên qua xuống Slot bên dưới
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Di chuyển hình ảnh theo chuột (Client side prediction - chỉ hiện thị)
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        // Logic thực sự sẽ nằm ở Slot khi nhận sự kiện OnDrop
        // Nếu không drop vào đâu hợp lý, quay về chỗ cũ
        transform.SetParent(parentAfterDrag);
        rectTransform.anchoredPosition = Vector2.zero;
    }
}