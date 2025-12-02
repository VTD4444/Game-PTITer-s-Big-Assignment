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
    
    [Header("Giao diện Minigame")]
    public GameObject minigamePanel; // Kéo Panel_Minigame_Code vào đây

    private bool isPlayerInside = false;

    void Start()
    {
        if (promptCanvas != null) promptCanvas.SetActive(false);
        // Đảm bảo minigame tắt lúc đầu (đề phòng quên chưa tắt ở editor)
        if (minigamePanel != null) minigamePanel.SetActive(false);
    }

    void Update()
    {
        // Chỉ cho phép bấm E khi đang đứng trong vùng VÀ Minigame chưa bật
        // (Hoặc nếu muốn bấm E lần nữa để tắt thì sửa logic ở đây)
        if (isPlayerInside && Input.GetKeyDown(KeyCode.E))
        {
            // Nếu panel chưa bật -> Bật lên
            if (minigamePanel != null && !minigamePanel.activeSelf)
            {
                OpenMinigame();
            }
            // Nếu panel đang bật -> Tắt đi
            else if (minigamePanel != null && minigamePanel.activeSelf)
            {
                CloseMinigame();
            }
        }
    }

    // --- HÀM MỞ MINIGAME ---
    public void OpenMinigame()
    {
        if (minigamePanel != null) minigamePanel.SetActive(true);
        if (promptCanvas != null) promptCanvas.SetActive(false); // Ẩn nút E cho đỡ vướng

        // Khóa di chuyển nhân vật
        if (PlayerController.LocalPlayerInstance != null)
        {
            PlayerController.LocalPlayerInstance.canMove = false;
        }
        
        Debug.Log("Đã mở Minigame: " + type);
    }

    // --- HÀM ĐÓNG MINIGAME (Gán vào nút X) ---
    public void CloseMinigame()
    {
        if (minigamePanel != null) minigamePanel.SetActive(false);
        
        // Nếu vẫn đứng trong vùng thì hiện lại nút E
        if (isPlayerInside && promptCanvas != null) promptCanvas.SetActive(true);

        // Mở khóa di chuyển nhân vật
        if (PlayerController.LocalPlayerInstance != null)
        {
            PlayerController.LocalPlayerInstance.canMove = true;
        }

        Debug.Log("Đã đóng Minigame");
    }

    // --- XỬ LÝ TRIGGER ---
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PhotonView pView = collision.GetComponent<PhotonView>();
            if (pView != null && pView.IsMine)
            {
                isPlayerInside = true;
                // Chỉ hiện nút E nếu đang KHÔNG chơi minigame
                if (minigamePanel != null && !minigamePanel.activeSelf)
                {
                    if (promptCanvas != null) promptCanvas.SetActive(true);
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PhotonView pView = collision.GetComponent<PhotonView>();
            if (pView != null && pView.IsMine)
            {
                isPlayerInside = false;
                if (promptCanvas != null) promptCanvas.SetActive(false);
                
                // Nếu chạy ra khỏi vùng mà quên tắt bảng -> Tự động tắt dùm
                CloseMinigame();
            }
        }
    }
}