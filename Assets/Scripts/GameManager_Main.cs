using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro; // Cần thiết để chỉnh sửa text điểm số
using UnityEngine.SceneManagement;

public class GameManager_Main : MonoBehaviourPunCallbacks
{
    public static GameManager_Main Instance;
    
    [Header("Tutorial UI")]
    public GameObject panelTutorial;

    [Header("Config")]
    public Transform[] spawnPoints; 
    public RuntimeAnimatorController[] allCharacterAnimators; 
    public Sprite[] allCharacterSprites; 

    [Header("End Game Panels")]
    public GameObject panelResult_A_Plus; // 10 điểm (Thủ khoa)
    public GameObject panelResult_B;      // 7.5 điểm (Qua môn)
    public GameObject panelResult_F_Time; // 0 điểm (Hết giờ)
    public GameObject panelResult_F_Fail; // Hỏng máy (Chết)

    // Biến kiểm tra điều kiện "Hoàn hảo" (Chưa từng bị cảnh báo)
    // Biến này cần được set thành true nếu bất kỳ ai bị tụt dưới 30%
    public bool hasWarningTriggered = false; 

    private bool isGameEnded = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (PhotonNetwork.IsConnected) SpawnPlayer();
        
        // Ẩn hết panel kết quả lúc đầu
        CloseAllResultPanels();
        ShowTutorial();
    }
    
    // --- HÀM XỬ LÝ TUTORIAL ---
    void ShowTutorial()
    {
        if (panelTutorial != null)
        {
            panelTutorial.SetActive(true);
            
            // Khóa di chuyển của nhân vật ngay khi vào game
            // Lưu ý: Cần đợi 1 chút để Player được sinh ra rồi mới khóa được
            StartCoroutine(LockMovementRoutine());
        }
    }
    
    // Coroutine để đảm bảo tìm thấy Player rồi mới khóa
    System.Collections.IEnumerator LockMovementRoutine()
    {
        yield return new WaitForSeconds(0.1f); // Chờ Player Spawn xong
        if (PlayerController.LocalPlayerInstance != null)
        {
            PlayerController.LocalPlayerInstance.canMove = false;
        }
    }
    
    // [MỚI] GẮN HÀM NÀY VÀO NÚT "ĐÃ HIỂU"
    public void OnClickCloseTutorial()
    {
        if (panelTutorial != null)
        {
            panelTutorial.SetActive(false);
        }

        // Mở khóa di chuyển để bắt đầu chơi
        if (PlayerController.LocalPlayerInstance != null)
        {
            PlayerController.LocalPlayerInstance.canMove = true;
        }
    }

    void SpawnPlayer()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return;
        int spawnIndex = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawnPoints.Length;
        Vector3 spawnPos = spawnPoints[spawnIndex].position;
        PhotonNetwork.Instantiate("Player", spawnPos, Quaternion.identity);
    }

    // Hàm gọi từ PlayerStats khi máu tụt xuống mức nguy hiểm
    public void TriggerWarningFlag()
    {
        if (!hasWarningTriggered)
        {
            hasWarningTriggered = true;
            // Có thể thêm dòng Debug để test: 
            // Debug.Log("Mất chuỗi hoàn hảo! Đã bị cảnh báo.");
        }
    }

    // ========================================================================
    // --- LOGIC KẾT THÚC GAME ---
    // ========================================================================

    // 1. GỌI KHI NGƯỜI CHƠI BẤM NỘP BÀI (UPLOAD)
    public void CheckWinCondition(float currentTime)
    {
        if (isGameEnded) return;

        // Logic check điều kiện
        // Giả sử currentTime là giờ trong game (ví dụ: 29.5 = 05:30 sáng)
        // Mốc 6h sáng = 30.0f (Start 20h = 20.0f)

        // Tính toán điểm số cơ bản (Logic giả định, bạn có thể thay đổi)
        float baseScore = 8.0f; // Mặc định làm xong là 8
        float bonus = 0f;
        float totalScore = 0f;

        // --- KỊCH BẢN 1: THỦ KHOA (Nộp trước 6h + Không cảnh báo) ---
        if (currentTime < 30.0f && !hasWarningTriggered) 
        {
            bonus = 2.0f;
            totalScore = 10.0f;
            ShowEndGamePanel(panelResult_A_Plus, baseScore, bonus, totalScore, "Nộp bài lúc 05:30 AM. Quá đỉnh!");
        }
        // --- KỊCH BẢN 2: QUA MÔN (Nộp kịp nhưng muộn HOẶC đã bị cảnh báo) ---
        else
        {
            // Nộp sớm vẫn được chút bonus nhưng không max
            if (currentTime < 30.0f) bonus = 0.5f; 
            else bonus = 0f; // Nộp sát giờ 7h (31.0f)

            totalScore = baseScore + bonus;
            // Giới hạn max 9.5 nếu không phải thủ khoa
            if (totalScore > 9.5f) totalScore = 9.5f; 

            ShowEndGamePanel(panelResult_B, baseScore, bonus, totalScore, "Hơi toát mồ hôi nhưng vẫn kịp giờ.");
        }

        EndGameCommon();
    }

    // 2. GỌI TỪ TIMEMANAGER KHI HẾT GIỜ (07:00)
    public void TriggerTimeOut()
    {
        if (isGameEnded) return;
        
        // --- KỊCH BẢN 3: TRƯỢT MÔN (Hết giờ) ---
        ShowEndGamePanel(panelResult_F_Time, 6.5f, 0f, 0f, "Đã quá 07:00 sáng mà chưa bấm Upload.");
        EndGameCommon();
    }

    // 3. GỌI TỪ PLAYERSTATS KHI CHỈ SỐ VỀ 0
    public void TriggerCriticalFailure(string reason)
    {
        if (isGameEnded) return;

        // --- KỊCH BẢN 4: SỰ CỐ NGHIÊM TRỌNG (Chết) ---
        // Lý do có thể là "Đột quỵ do kiệt sức" hoặc "Phát điên vì bug"
        ShowEndGamePanel(panelResult_F_Fail, 0f, 0f, 0f, reason);
        EndGameCommon();
    }

    // --- HÀM HỖ TRỢ HIỂN THỊ ---
    void ShowEndGamePanel(GameObject panel, float score, float bonus, float total, string reason)
    {
        if (panel == null) return;
        panel.SetActive(true);

        // Tìm các Text con để điền số liệu (Dựa theo ảnh bạn gửi)
        // Lưu ý: Bạn cần đặt tên GameObject Text trong Unity đúng như code dưới đây
        // Hoặc kéo thả biến public nếu muốn an toàn hơn. Ở đây mình dùng Find cho gọn.

        TextMeshProUGUI txtScore = panel.transform.Find("ScoreRow/Value")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI txtBonus = panel.transform.Find("BonusRow/Value")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI txtTotal = panel.transform.Find("TotalRow/Value")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI txtReason = panel.transform.Find("ReasonBox/Text")?.GetComponent<TextMeshProUGUI>();
        
        // Hiển thị điểm to đùng ở giữa (vòng tròn)
        TextMeshProUGUI txtBigScore = panel.transform.Find("CircleScore/Value")?.GetComponent<TextMeshProUGUI>();

        if (txtScore) txtScore.text = (score > 0) ? score.ToString("F1") : "ERROR";
        if (txtBonus) 
        {
            if (bonus > 0) txtBonus.text = "+" + bonus.ToString("F1");
            else txtBonus.text = (total == 0) ? "FAIL" : "---";
        }
        if (txtTotal) txtTotal.text = total.ToString("F1");
        if (txtReason) txtReason.text = reason;
        if (txtBigScore) txtBigScore.text = total.ToString("F1"); // Ví dụ: 10
    }

    void EndGameCommon()
    {
        isGameEnded = true;
        // Dừng thời gian, chặn di chuyển, v.v...
        Time.timeScale = 0f; // Tạm dừng game
    }

    void CloseAllResultPanels()
    {
        if(panelResult_A_Plus) panelResult_A_Plus.SetActive(false);
        if(panelResult_B) panelResult_B.SetActive(false);
        if(panelResult_F_Time) panelResult_F_Time.SetActive(false);
        if(panelResult_F_Fail) panelResult_F_Fail.SetActive(false);
    }
    
    // Giữ nguyên hàm ChangeSkin cũ của bạn
    public void ChangeSkin(PlayerController player, int index)
    {
        if (allCharacterAnimators != null && index >= 0 && index < allCharacterAnimators.Length)
        {
            if (allCharacterAnimators[index] != null) player.anim.runtimeAnimatorController = allCharacterAnimators[index];
        }
        if (allCharacterSprites != null && index >= 0 && index < allCharacterSprites.Length)
        {
            if (allCharacterSprites[index] != null) player.spriteRenderer.sprite = allCharacterSprites[index];
        }
    }
    public void OnClickBackToMenu()
    {
        // 1. Quan trọng: Reset lại thời gian game
        // (Vì khi hiện bảng Win/Loss, bạn đã set Time.timeScale = 0)
        Time.timeScale = 1f; 

        // 2. Rời phòng Photon hiện tại
        Debug.Log("Đang rời phòng...");
        PhotonNetwork.LeaveRoom();
    }

    // 3. Callback tự động chạy khi rời phòng thành công
    public override void OnLeftRoom()
    {
        Debug.Log("Đã rời phòng. Đang về Lobby.");
        
        // 4. Load lại Scene sảnh chờ
        // Hãy đổi "Lobby" thành tên chính xác của Scene chứa LobbyManager của bạn
        SceneManager.LoadScene("Lobby"); 
    }
}