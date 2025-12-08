using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MinigameLoveMess : MonoBehaviour
{
    public static MinigameLoveMess Instance;

    [Header("UI Components")]
    public TextMeshProUGUI txtNYMessage; // Text hiển thị tin nhắn của NY
    public GameObject buttonContainer;   // Chứa 3 nút chọn
    public Button btnOptionA;            // Nút lựa chọn 1 (Trên cùng)
    public Button btnOptionB;            // Nút lựa chọn 2 (Giữa)
    public Button btnOptionC;            // Nút lựa chọn 3 (Dưới)
    public TextMeshProUGUI txtOptionA;   // Text của nút 1
    public TextMeshProUGUI txtOptionB;   // Text của nút 2
    public TextMeshProUGUI txtOptionC;   // Text của nút 3

    [Header("Spam Effect UI")]
    public GameObject panelSpamOverlay;  // Panel đỏ nhấp nháy khi bị spam
    public Button btnPowerOff;           // Nút tắt nguồn để chặn spam

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioSource bgmSource;
    public AudioClip sfxMessage;    // Tiếng tin nhắn đến
    public AudioClip sfxShock;      // Tiếng sốc (Fail A)
    public AudioClip sfxSpam;       // Tiếng tin nhắn dồn dập (Fail B)
    public AudioClip sfxWin;        // Tiếng chiến thắng (Win C)
    public AudioClip sfxPowerOff;   // Tiếng tắt nguồn

    // Link nhạc youtube bạn gửi, bạn cần tải về file mp3/wav và gán vào đây
    public AudioClip bgmSad;        // Nhạc buồn (Violin)
    public AudioClip bgmStress;     // Nhạc căng thẳng
    public AudioClip bgmVictory;    // Nhạc chiến thắng
    
    public static MinigameLoveMess instance;

    // Trạng thái hội thoại
    private enum State { 
        Intro, 
        // Nhánh A - Thành thật
        A_Ask1, A_Ask2, A_Fail_Final, 
        // Nhánh B - Cục súc
        B_Argue1, B_Argue2, B_Fail_Spam, 
        // Nhánh C - Lươn lẹo
        C_Flatter1, C_Flatter2, C_Win 
    }
    
    private State currentState;
    private bool isSpamming = false; // Biến kiểm tra trạng thái bị spam tin nhắn

    void Awake()
    {
        Instance = this;
        // Gán sự kiện cho các nút
        btnOptionA.onClick.AddListener(() => OnOptionSelected(0));
        btnOptionB.onClick.AddListener(() => OnOptionSelected(1));
        btnOptionC.onClick.AddListener(() => OnOptionSelected(2));
        btnPowerOff.onClick.AddListener(ShutdownPhone);
    }

    void OnEnable()
    {
        // Reset trạng thái khi mở Minigame
        ResetMinigame();
    }

    void Update()
    {
        // Logic trừ Energy khi bị Spam tin nhắn (Nhánh B)
        if (isSpamming)
        {
            if (PlayerStats.Instance != null)
            {
                // Trừ 5 Energy mỗi giây
                PlayerStats.LocalInstance.RestoreEnergy(-15f * Time.deltaTime);
                
                // Rung màn hình hoặc hiệu ứng visual nếu cần
            }
        }
    }

    void ResetMinigame()
    {
        currentState = State.Intro;
        isSpamming = false;
        if (panelSpamOverlay) panelSpamOverlay.SetActive(false);
        if (btnPowerOff) btnPowerOff.gameObject.SetActive(false);
        buttonContainer.SetActive(true);
        
        // Bắt đầu hội thoại
        ShowDialogue_Intro();
    }

    // --- HỆ THỐNG HIỂN THỊ ---
    
    // Hàm hiển thị tin nhắn và các lựa chọn
    void SetUI(string nyMsg, string optA, string optB = "", string optC = "")
    {
        txtNYMessage.text = "NY: " + nyMsg;
        
        txtOptionA.text = optA;
        btnOptionA.gameObject.SetActive(true);

        if (!string.IsNullOrEmpty(optB))
        {
            txtOptionB.text = optB;
            btnOptionB.gameObject.SetActive(true);
        }
        else btnOptionB.gameObject.SetActive(false);

        if (!string.IsNullOrEmpty(optC))
        {
            txtOptionC.text = optC;
            btnOptionC.gameObject.SetActive(true);
        }
        else btnOptionC.gameObject.SetActive(false);

        // Play Sound
        if(sfxSource && sfxMessage) sfxSource.PlayOneShot(sfxMessage);
    }

    // --- LOGIC XỬ LÝ LỰA CHỌN ---

    void OnOptionSelected(int index)
    {
        switch (currentState)
        {
            case State.Intro:
                if (index == 0) StartBranch_A(); // Thành thật
                else if (index == 1) StartBranch_B(); // Cục súc
                else if (index == 2) StartBranch_C(); // Lươn lẹo
                break;

            // --- XỬ LÝ NHÁNH A (Thành thật -> Toang) ---
            case State.A_Ask1:
                // Nam vừa hỏi "Sinh nhật em qua rồi mà?"
                // NY rep: "Qua 2 tháng rồi..." -> Player chọn tiếp
                ShowDialogue_A_Step2(); 
                break;
            case State.A_Ask2:
                // Nam đoán sai tiếp -> NY chốt hạ
                EndGame_A_Fail();
                break;

            // --- XỬ LÝ NHÁNH B (Cục súc -> Spam) ---
            case State.B_Argue1:
                ShowDialogue_B_Step2();
                break;
            case State.B_Argue2:
                EndGame_B_Spam();
                break;

            // --- XỬ LÝ NHÁNH C (Lươn lẹo -> Win) ---
            case State.C_Flatter1:
                ShowDialogue_C_Step2();
                break;
            case State.C_Flatter2:
                EndGame_C_Win();
                break;
        }
    }

    // --- NỘI DUNG HỘI THOẠI (KỊCH BẢN) ---

    void ShowDialogue_Intro()
    {
        SetUI("Anh lại quên ngày kỉ niệm đúng không? 🙂",
            "A. Ơ.. hôm nay là ngày gì ấy nhỉ?", 
            "B. Anh đang chạy deadline sấp mặt đây, mai nói sau.", 
            "C. Quên thế nào được, đang cày code để mai đi chơi nè ❤️");
    }

    // ========== NHÁNH A: THÀNH THẬT (Failure) ==========
    void StartBranch_A()
    {
        currentState = State.A_Ask1;
        // Sáng tạo thêm lựa chọn ở đây để người chơi cảm thấy mình đang "giãy chết"
        SetUI("Sinh nhật em qua 2 tháng rồi anh ạ. Anh đùa em đấy à?",
            "Thế kỉ niệm 1 năm yêu nhau?",
            "Hay kỉ niệm lần đầu mình... nắm tay?");
    }

    void ShowDialogue_A_Step2()
    {
        currentState = State.A_Ask2;
        SetUI("1 năm mình mới kỷ niệm tháng trước rồi. Anh bị mất trí nhớ hay cố tình không để tâm?",
            "Thế..lần đầu tiên chúng mình lướt qua nhau ở nhà xe?", // Câu gốc
            "Thôi anh chịu, em gợi ý đi mà..."); // Câu sáng tạo thêm
    }

    void EndGame_A_Fail()
    {
        currentState = State.A_Fail_Final;
        txtNYMessage.text = "NY: Sai. Hôm nay là kỷ niệm 1 năm 1 tháng 1 ngày. Số đẹp thế mà không để ý. CHIA TAY ĐI! (Bạn đã bị chặn)";
        buttonContainer.SetActive(false); // Ẩn nút chọn

        // Hiệu ứng Fail A
        StartCoroutine(ShockEffect());
    }

    IEnumerator ShockEffect()
    {
        // 1. Play SFX Sốc
        if(sfxSource && sfxShock) sfxSource.PlayOneShot(sfxShock);
        
        // 2. Đổi nhạc nền
        if(bgmSource && bgmSad) { bgmSource.clip = bgmSad; bgmSource.Play(); }

        // 3. Đóng băng (Logic game) - Giả sử pause logic hoặc chặn input
        Time.timeScale = 0; // Tạm dừng game để tạo cảm giác sốc
        yield return new WaitForSecondsRealtime(2f); // Chờ 2s thời gian thực
        Time.timeScale = 1;

        // HÌNH PHẠT: Giảm 50 Sanity
        if (PlayerStats.LocalInstance != null)
        {
            // Truyền số ÂM để trừ
            PlayerStats.LocalInstance.RestoreSanity(-50f); 
            Debug.Log("Đã trừ 50 Sanity!");
        }

        yield return new WaitForSeconds(3f);
        CloseMinigame();
    }


    // ========== NHÁNH B: CỤC SÚC (Stress Spam) ==========
    void StartBranch_B()
    {
        currentState = State.B_Argue1;
        SetUI("Mai? Mai của anh là sang kỉ niệm khác rồi.",
            "Nhưng giờ anh đang bận thật, em trật tự cho anh làm.",
            "Em đừng có trẻ con thế được không?");
    }

    void ShowDialogue_B_Step2()
    {
        currentState = State.B_Argue2;
        SetUI("Anh quát em à? Ngày xưa anh thức tới 3h sáng tán tỉnh em cơ mà?",
            "Ngày xưa anh không bận làm BTL!",
            "Thôi được rồi, anh xin lỗi, nhưng để anh làm nốt đã.");
    }

    void EndGame_B_Spam()
    {
        currentState = State.B_Fail_Spam;
        // Bắt đầu chuỗi spam
        txtNYMessage.text = "NY: Vậy anh cưới luôn máy tính đi nhé! \n(Messages Incoming...)";
        buttonContainer.SetActive(false);
        
        // Kích hoạt chế độ Spam
        isSpamming = true;
        if(panelSpamOverlay) panelSpamOverlay.SetActive(true);
        if(btnPowerOff) btnPowerOff.gameObject.SetActive(true); // Hiện nút tắt nguồn

        // Audio
        if(bgmSource && bgmStress) { bgmSource.clip = bgmStress; bgmSource.Play(); }
        if(sfxSource && sfxSpam) { sfxSource.clip = sfxSpam; sfxSource.loop = true; sfxSource.Play(); }
    }

    void ShutdownPhone()
    {
        // Player bấm nút tắt nguồn để cứu vãn cuộc đời
        isSpamming = false;
        if(sfxSource) { sfxSource.Stop(); sfxSource.PlayOneShot(sfxPowerOff); }
        
        StartCoroutine(CloseDelay(1f));
    }


    // ========== NHÁNH C: LƯƠN LẸO (Victory) ==========
    void StartBranch_C()
    {
        currentState = State.C_Flatter1;
        // Sáng tạo thêm
        SetUI("Thật không đó? Hay anh lại văn vở trốn chơi game với anh Bình?",
            "Oan quá. Anh vừa code vừa nghĩ xem mai đi ăn gì đây này.",
            "Anh thề! Anh mà nói điêu bug tràn ngập project luôn!");
    }

    void ShowDialogue_C_Step2()
    {
        currentState = State.C_Flatter2;
        SetUI("Nghe cũng xuôi tai đấy, nhưng anh đang làm gì đấy?",
            "Anh đang code BTL bằng cả trái tim ❤️",
            "Đang fix bug để mai có thời gian bên em nè.");
    }

    void EndGame_C_Win()
    {
        currentState = State.C_Win;
        txtNYMessage.text = "NY: Ỏ, dạ, cố lên nha anh ❤️";
        buttonContainer.SetActive(false);

        // Hiệu ứng Win
        if(bgmSource && bgmVictory) { bgmSource.clip = bgmVictory; bgmSource.Play(); }
        if(sfxSource && sfxWin) sfxSource.PlayOneShot(sfxWin);

        // PHẦN THƯỞNG: Tăng 30 Sanity
        if (PlayerStats.LocalInstance != null)
        {
            // Truyền số DƯƠNG để cộng
            PlayerStats.LocalInstance.RestoreSanity(30f); 
            Debug.Log("Đã hồi 30 Sanity!");
        }

        StartCoroutine(CloseDelay(4f));
    }

    // --- TIỆN ÍCH ---
    IEnumerator CloseDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        CloseMinigame();
    }

    public void CloseMinigame()
    {
        // Gọi hàm CloseAllMinigames từ script InteractableObject hoặc tắt gameObject này
        // Vì script này nằm trên Panel, ta chỉ cần tắt nó đi
        gameObject.SetActive(false);
        
        // Reset nhạc nền về bình thường (Gọi code AudioManager của bạn)
        // AudioManager.Instance.PlayBackgroundMusic(); 
        
        // Mở lại khả năng di chuyển cho Player (Quan trọng)
        if (PlayerController.LocalPlayerInstance != null) 
             PlayerController.LocalPlayerInstance.canMove = true;
    }
}