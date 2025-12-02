using UnityEngine;
using UnityEngine.UI; // Để dùng Slider
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable; // Để lưu Code Progress

public class PlayerStats : MonoBehaviourPun
{
    [Header("Cấu hình Chỉ số")]
    public float maxEnergy = 100f;
    public float maxSanity = 100f;
    public float decayRate = 0.01f; // Tốc độ tụt chỉ số mỗi giây

    [Header("Chỉ số hiện tại (Read Only)")]
    public float currentEnergy;
    public float currentSanity;

    // Tham chiếu đến UI (Sẽ tự tìm)
    private Slider codeSlider;
    private Slider energySlider;
    private Slider sanitySlider;

    // Key để lưu trữ Tiến độ Code trên mạng
    private const string CODE_PROGRESS_KEY = "CodeProgress";

    void Start()
    {
        // Khởi tạo chỉ số đầy
        currentEnergy = maxEnergy;
        currentSanity = maxSanity;

        if (photonView.IsMine)
        {
            // Tự động tìm các thanh Slider trên màn hình HUD
            FindSliders();
        }
    }

    void Update()
    {
        // Chỉ tính toán cho nhân vật CỦA TÔI
        if (photonView.IsMine)
        {
            // 1. Giảm chỉ số theo thời gian (Sinh tồn)
            DecreaseStats();

            // 2. Cập nhật hiển thị lên UI
            UpdateUI();
        }
    }

    void DecreaseStats()
    {
        // Trừ dần theo thời gian thực
        if (currentEnergy > 0) currentEnergy -= decayRate * Time.deltaTime;
        if (currentSanity > 0) currentSanity -= decayRate * Time.deltaTime;
    }

    void UpdateUI()
    {
        if (energySlider != null) energySlider.value = currentEnergy / maxEnergy;
        if (sanitySlider != null) sanitySlider.value = currentSanity / maxSanity;

        // Lấy tiến độ Code từ Phòng (Room Properties) để cập nhật thanh chung
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(CODE_PROGRESS_KEY))
        {
            float progress = (float)PhotonNetwork.CurrentRoom.CustomProperties[CODE_PROGRESS_KEY];
            if (codeSlider != null) codeSlider.value = progress / 100f;
        }
    }

    void FindSliders()
    {
        // Tìm Canvas_HUD đang có sẵn trong Scene
        GameObject canvas = GameObject.Find("Canvas_HUD");
        if (canvas != null)
        {
            // Tìm các Slider theo tên bạn đặt ở Bước 1
            // Dùng transform.Find đệ quy hoặc tìm theo Tag sẽ an toàn hơn, 
            // nhưng ở đây ta tìm theo tên object cho đơn giản.
            Slider[] allSliders = canvas.GetComponentsInChildren<Slider>();
            
            foreach(Slider s in allSliders)
            {
                if (s.name == "Slider_Code") codeSlider = s;
                if (s.name == "Slider_Energy") energySlider = s;
                if (s.name == "Slider_Sanity") sanitySlider = s;
            }
        }
    }

    // --- HÀM TĂNG TIẾN ĐỘ CODE (Sẽ dùng cho Minigame) ---
    public void AddCodeProgress(float amount)
    {
        if (PhotonNetwork.IsMasterClient) // Chỉ chủ phòng mới được quyền ghi đè dữ liệu phòng để tránh xung đột
        {
            float currentProg = 0;
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(CODE_PROGRESS_KEY))
            {
                currentProg = (float)PhotonNetwork.CurrentRoom.CustomProperties[CODE_PROGRESS_KEY];
            }
            
            currentProg += amount;
            if (currentProg > 100) currentProg = 100;
            if (currentProg < 0) currentProg = 0;
            Debug.Log(currentProg);

            Hashtable props = new Hashtable { { CODE_PROGRESS_KEY, currentProg } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
        else
        {
            // Nếu không phải chủ phòng, gửi yêu cầu RPC hoặc tin nhắn (tạm thời ta bỏ qua bước phức tạp này, cứ để ai cũng ghi được cho đơn giản ở giai đoạn này)
            float currentProg = 0;
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(CODE_PROGRESS_KEY))
            {
                currentProg = (float)PhotonNetwork.CurrentRoom.CustomProperties[CODE_PROGRESS_KEY];
            }
            currentProg += amount;
             Hashtable props = new Hashtable { { CODE_PROGRESS_KEY, currentProg } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
    }
    
    // --- HÀM HỒI PHỤC (Dùng cho Nấu ăn/Ngủ) ---
    public void RestoreEnergy(float amount)
    {
        currentEnergy += amount;
        if (currentEnergy > maxEnergy) currentEnergy = maxEnergy;
    }

     public void RestoreSanity(float amount)
    {
        currentSanity += amount;
        if (currentSanity > maxSanity) currentSanity = maxSanity;
    }
}