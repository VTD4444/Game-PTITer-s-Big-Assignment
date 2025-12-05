using UnityEngine;
using Photon.Pun;

public enum InteractionType { Code, Cook, FixWifi, Sleep }

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
        if (isPlayerInside && Input.GetKeyDown(KeyCode.E))
        {
            // Kiểm tra xem có bất kỳ panel nào đang mở không
            bool isAnyPanelOpen = (panelHello != null && panelHello.activeSelf) || 
                                  (panelFlow != null && panelFlow.activeSelf) ||
                                  (panelMech != null && panelMech.activeSelf);

            // Nếu đang mở bất kỳ cái nào -> KHÔNG LÀM GÌ CẢ (Chặn phím E)
            if (isAnyPanelOpen) return; 

            // Nếu chưa mở -> Thì mới mở
            OpenCorrectMinigame();
        }
    }

    void OpenCorrectMinigame()
    {
        float currentProg = 0;
        if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("CodeProgress"))
        {
            currentProg = (float)PhotonNetwork.CurrentRoom.CustomProperties["CodeProgress"];
        }

        if (type == InteractionType.Code)
        {
            ActivatePanel(panelDecode);
            // --- LOGIC CHUYỂN GIAI ĐOẠN ---
            // if (currentProg < 25f)
            // {
            //     ActivatePanel(panelHello);
            // }
            // else if (currentProg < 50f)
            // {
            //     ActivatePanel(panelFlow);
            // }
            // else if (currentProg < 75f) // GIAI ĐOẠN 3
            // {
            //     ActivatePanel(panelMech);
            // }
            // else if (currentProg < 100f) ActivatePanel(panelDecode);
            
        }
        else if (type == InteractionType.Cook)
        {
            // Mở game nấu ăn (sẽ làm sau)
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
        if (panelHello) panelHello.SetActive(false);
        if (panelFlow) panelFlow.SetActive(false);
        if (panelMech) panelMech.SetActive(false); 
        if (panelDecode) panelDecode.SetActive(false);

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
}