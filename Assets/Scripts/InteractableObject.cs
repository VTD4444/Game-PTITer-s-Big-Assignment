using UnityEngine;
using Photon.Pun;

public enum InteractionType { Code, Cook, FixWifi, Toilet, Fridge, Barista, Phone }

public class InteractableObject : MonoBehaviour
{
    [Header("Cấu hình")]
    public InteractionType type;
    public GameObject promptCanvas; 
    
    [Header("Danh sách Minigame")]
    public GameObject panelHello; // Game 1 (0-25%)
    public GameObject panelFlow;  // Game 2 (25-50%)
    public GameObject panelMech;  // Game 3 (50-75%)
    public GameObject panelDecode;
    public GameObject panelLoveMess;

    private bool isPlayerInside = false;

    void Start()
    {
        if (promptCanvas != null) promptCanvas.SetActive(false);
        
        // Đảm bảo tắt hết các panel lúc đầu
        if (panelHello != null) panelHello.SetActive(false);
        if (panelFlow != null) panelFlow.SetActive(false);
        if (panelMech != null) panelMech.SetActive(false);
    }

    void Update()
    {
        // Chỉ hiện Prompt E khi ở gần và chưa mở game nào
        if (isPlayerInside && promptCanvas != null)
        {
            // Logic ẩn hiện nút E thông minh hơn:
            // Nếu là Phone: Chỉ hiện E khi có sự kiện (LoveMessManager.IsEventActive = true)
            if (type == InteractionType.Phone)
            {
                // [YÊU CẦU 2] Kiểm tra xem có phải Host (MasterClient) không?
                if (PhotonNetwork.IsMasterClient)
                {
                    // Nếu chủ phòng -> Bật nút E
                    if(promptCanvas) promptCanvas.SetActive(false);
                    if(promptCanvas) promptCanvas.SetActive(true);
                }
            }
            else 
            {
                // Các đồ vật khác hiện E bình thường
                promptCanvas.SetActive(true);
            }
        }

        if (isPlayerInside && Input.GetKeyDown(KeyCode.E))
        {
            if (type == InteractionType.Phone && !PhotonNetwork.IsMasterClient)
            {
                Debug.Log("Bạn không phải chủ phòng, không thể nghe điện thoại!");
                return;
            }
            if (IsAnyPanelOpen()) return; // Đang mở bảng khác thì chặn
            OpenCorrectMinigame();
        }
    }

    bool IsAnyPanelOpen()
    {
        return (panelHello && panelHello.activeSelf) || 
               (panelFlow && panelFlow.activeSelf) ||
               (panelMech && panelMech.activeSelf) ||
               (panelLoveMess && panelLoveMess.activeSelf);
    }

    void OpenCorrectMinigame()
    {
        // --- KIỂM TRA BỊ SICK ---
        if (CookingManager.Instance.IsSick)
        {
            switch (type)
            {
                case InteractionType.Toilet:
                    CookingManager.Instance.OpenToiletInteraction();
                    break;

                default:
                    Debug.Log("Đau bụng quá! Phải tìm Toilet!");
                    break;
            }
            return;
        }

        // --- LẤY CODE PROGRESS ---
        float currentProg = 0;
        if (PhotonNetwork.CurrentRoom != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("CodeProgress"))
        {
            currentProg = (float)PhotonNetwork.CurrentRoom.CustomProperties["CodeProgress"];
        }

        // --- SWITCH THEO LOẠI TƯƠNG TÁC ---
        switch (type)
        {
            case InteractionType.Code:
                if (WifiManager.Instance != null && WifiManager.Instance.IsWifiBroken)
                {
                    Debug.Log("Wifi đang hỏng! Không thể code!");
                    WifiManager.Instance.OpenPCPanel(); // Bật màn hình báo lỗi thay vì game Code
                    return; // Dừng lại, không mở minigame code nữa
                }
                // Chuyển 4 giai đoạn bằng if (vì nó phụ thuộc giá trị float)
                if (currentProg < 25f)
                {
                    ActivatePanel(panelHello);
                }
                else if (currentProg < 50f)
                {
                    ActivatePanel(panelFlow);
                }
                else if (currentProg < 75f)
                {
                    ActivatePanel(panelMech);
                }
                else if (currentProg < 100f)
                {
                    ActivatePanel(panelDecode);
                }
                break;

            case InteractionType.Cook:
                CookingManager.Instance.OpenKitchenInteraction();
                break;

            case InteractionType.Fridge:
                CookingManager.Instance.OpenFridge();
                break;

            case InteractionType.Barista:
                BaristaManager.Instance.OpenBaristaGame();
                break;

            case InteractionType.Toilet:
                // Trường hợp này chỉ chạy khi không bị Sick
                CookingManager.Instance.OpenToiletInteraction();
                break;
            
            case InteractionType.FixWifi:
                // Chỉ mở được nếu Wifi đang hỏng
                if (WifiManager.Instance.IsWifiBroken)
                {
                    WifiManager.Instance.OpenRouterPanel();
                }
                break;
            case InteractionType.Phone:
                // Kiểm tra xem có đang diễn ra sự kiện tin nhắn không?
                // Chúng ta sẽ cần một Manager quản lý trạng thái cái điện thoại (Xem Bước 2)
                if (panelLoveMess)
                {
                    // [MỚI] Nếu đang có sự kiện -> Gọi nghe máy để tắt chuông
                    if (LoveMessManager.Instance != null && LoveMessManager.Instance.IsEventActive)
                    {
                        LoveMessManager.Instance.PickupPhone();
                    }
                    ActivatePanel(panelLoveMess);
                }
                break;

            default:
                Debug.Log("Không có minigame tương ứng với InteractionType này.");
                break;
        }
    }


    void ActivatePanel(GameObject panel)
    {
        if (panel != null)
        {
            panel.SetActive(true);
            if (promptCanvas != null) promptCanvas.SetActive(false);
            
            // Khóa di chuyển
            if (PlayerController.LocalPlayerInstance != null) 
                PlayerController.LocalPlayerInstance.canMove = false;
        }
    }

    // Hàm gọi từ nút X (Close Button) của TẤT CẢ các Panel
    public void CloseAllMinigames()
    {
        Debug.Log("Closing all Minigames");
        if (panelHello) panelHello.SetActive(false);
        if (panelFlow) panelFlow.SetActive(false);
        if (panelMech) panelMech.SetActive(false); 
        if (panelDecode) panelDecode.SetActive(false);
        if (panelLoveMess) panelLoveMess.SetActive(false);

        // Mở lại nút E
        if (isPlayerInside && promptCanvas) promptCanvas.SetActive(true);
        
        // Mở khóa di chuyển
        if (PlayerController.LocalPlayerInstance) 
            PlayerController.LocalPlayerInstance.canMove = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && collision.GetComponent<PhotonView>().IsMine)
        {
            isPlayerInside = true;
            
            bool isAnyPanelOpen = (panelHello != null && panelHello.activeSelf) || 
                                  (panelFlow != null && panelFlow.activeSelf) ||
                                  (panelMech != null && panelMech.activeSelf);

            if (!isAnyPanelOpen && promptCanvas != null) promptCanvas.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && collision.GetComponent<PhotonView>().IsMine)
        {
            isPlayerInside = false;
            if (promptCanvas != null) promptCanvas.SetActive(false);
            CloseAllMinigames(); 
        }
    }
    
    // Hàm kiểm tra xem người chơi có đang mở bất kỳ minigame code nào không
    public bool IsAnyCodeMinigameActive()
    {
        if (type != InteractionType.Code) return false;

        // Kiểm tra trạng thái active của tất cả các panel minigame
        if (panelHello && panelHello.activeSelf) return true;
        if (panelFlow && panelFlow.activeSelf) return true;
        if (panelMech && panelMech.activeSelf) return true;
        if (panelDecode && panelDecode.activeSelf) return true;

        return false;
    }
}