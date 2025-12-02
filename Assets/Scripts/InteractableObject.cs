using UnityEngine;
using Photon.Pun;
using TMPro;

public enum InteractionType
{
    Code,
    Cook,
    FixWifi,
    Sleep
}

public class InteractableObject : MonoBehaviour
{
    [Header("Cấu hình")]
    public InteractionType type;
    public GameObject promptCanvas; // Nút E
    
    [Header("Danh sách Minigame")]
    public GameObject panelHello; // Game 1 (Hello World)
    public GameObject panelFlow;

    private bool isPlayerInside = false;

    void Start()
    {
        if (promptCanvas != null) promptCanvas.SetActive(false);
        if (panelHello != null) panelHello.SetActive(false);
        if (panelFlow != null) panelFlow.SetActive(false);
    }

    void Update()
    {
        // Chỉ cho phép bấm E khi đang đứng trong vùng VÀ Minigame chưa bật
        if (isPlayerInside && Input.GetKeyDown(KeyCode.E))
        {
            OpenCorrectMinigame();
        }
    }

    void OpenCorrectMinigame()
    {
        // Lấy tiến độ code từ mạng
        float currentProg = 0;
        if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("CodeProgress"))
        {
            currentProg = (float)PhotonNetwork.CurrentRoom.CustomProperties["CodeProgress"];
        }

        // --- LOGIC CHUYỂN GAME THEO TIẾN ĐỘ ---
        if (type == InteractionType.Code)
        {
            ActivatePanel(panelFlow);
            // if (currentProg < 25f)
            // {
            //     ActivatePanel(panelHello); // Dưới 25%: Chơi Hello World
            // }
            // else if (currentProg < 50f)
            // {
            //     ActivatePanel(panelFlow);  // 25% - 50%: Chơi In The Flow
            // }
            // else
            // {
            //     Debug.Log("Giai đoạn 3 chưa làm!"); // Sau này làm tiếp
            // }
        }
    }
    
    void ActivatePanel(GameObject panel)
    {
        if (panel != null)
        {
            panel.SetActive(true);
            if (promptCanvas != null) promptCanvas.SetActive(false);
            if (PlayerController.LocalPlayerInstance != null) PlayerController.LocalPlayerInstance.canMove = false;
        }
    }

    // --- HÀM ĐÓNG MINIGAME (Gán vào nút X) ---
    public void CloseAllMinigames()
    {
        if (panelHello != null) panelHello.SetActive(false);
        if (panelFlow != null) panelFlow.SetActive(false);

        if (isPlayerInside && promptCanvas != null) promptCanvas.SetActive(true);
        if (PlayerController.LocalPlayerInstance != null) PlayerController.LocalPlayerInstance.canMove = true;
    }

    // --- XỬ LÝ TRIGGER ---
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && collision.GetComponent<PhotonView>().IsMine)
        {
            isPlayerInside = true;
            bool isAnyPanelOpen = (panelHello != null && panelHello.activeSelf) || (panelFlow != null && panelFlow.activeSelf);
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