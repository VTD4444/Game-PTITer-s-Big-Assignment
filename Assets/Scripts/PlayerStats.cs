using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro; // [BẮT BUỘC] Thêm thư viện này để dùng TextMeshPro
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PlayerStats : MonoBehaviourPun
{
    public static PlayerStats LocalInstance;
    public static PlayerStats Instance;

    [Header("Cấu hình Chỉ số")]
    public float maxEnergy = 100f;
    public float maxSanity = 100f;
    public float decayRate = 0.5f;

    [Header("Cấu hình Cảnh báo")]
    public GameObject headTextPrefab; // Kéo Prefab "Player Head Text" vào đây
    public Vector3 headTextOffset = new Vector3(0, 1.5f, 0); // Vị trí text cao hơn đầu nhân vật

    [Header("Chỉ số hiện tại (Read Only)")]
    public float currentEnergy;
    public float currentSanity;

    // Tham chiếu đến UI HUD
    private Slider codeSlider;
    private Slider energySlider;
    private Slider sanitySlider;
    
    // [MỚI] Tham chiếu đến Text % trên HUD
    private TextMeshProUGUI energyPercentText;
    private TextMeshProUGUI sanityPercentText;

    // Biến quản lý Text trên đầu
    private GameObject myHeadTextInstance;
    private TextMeshProUGUI myHeadTextContent;
    private const string CODE_PROGRESS_KEY = "CodeProgress";

    void Awake()
    {
        if (photonView.IsMine)
        {
            LocalInstance = this;
        }
        Instance = this;
    }

    void Start()
    {
        currentEnergy = maxEnergy;
        currentSanity = maxSanity;

        if (photonView.IsMine)
        {
            FindSlidersAndTexts(); // Tìm cả Slider và Text %
            InitHeadText();        // Khởi tạo text trên đầu
        }
    }

    void Update()
    {
        if (photonView.IsMine)
        {
            DecreaseStats();
            UpdateUI();
            CheckWarnings(); // [MỚI] Kiểm tra cảnh báo
            if (currentEnergy < 30 || currentSanity < 30)
            {
                if (GameManager_Main.Instance) 
                    GameManager_Main.Instance.TriggerWarningFlag();
            }

            // Kiểm tra Chết (Kịch bản 4)
            if (currentEnergy <= 0)
            {
                if (GameManager_Main.Instance)
                    GameManager_Main.Instance.TriggerCriticalFailure("Lý do: Đột quỵ do kiệt sức (Hết Energy).");
            }
            else if (currentSanity <= 0)
            {
                if (GameManager_Main.Instance)
                    GameManager_Main.Instance.TriggerCriticalFailure("Lý do: Máy tính phát nổ do quá tải Code (Hết Sanity).");
            }
        }
    }

    // --- 1. LOGIC KHỞI TẠO TEXT TRÊN ĐẦU ---
    void InitHeadText()
    {
        if (headTextPrefab != null)
        {
            // Tạo ra Prefab làm con của nhân vật
            myHeadTextInstance = Instantiate(headTextPrefab, transform);
            myHeadTextInstance.transform.localPosition = headTextOffset;
            
            // Tìm component Text bên trong (giả sử bạn đã tạo TextMeshPro con như hướng dẫn Bước 1)
            myHeadTextContent = myHeadTextInstance.GetComponentInChildren<TextMeshProUGUI>();
            
            // Mặc định ẩn đi
            myHeadTextInstance.SetActive(false);
        }
    }

    // --- 2. LOGIC KIỂM TRA CẢNH BÁO (<30%) ---
    void CheckWarnings()
    {
        if (myHeadTextInstance == null || myHeadTextContent == null) return;

        float energyPercent = (currentEnergy / maxEnergy) * 100f;
        float sanityPercent = (currentSanity / maxSanity) * 100f;

        string warningMsg = "";

        // Ưu tiên cảnh báo: Đói -> Điên (Hoặc ngược lại tùy bạn)
        if (energyPercent < 30f)
        {
            warningMsg = "Bạn đang bị đói,\nhãy đến bếp để nấu ăn!";
        }
        else if (sanityPercent < 30f)
        {
            warningMsg = "Bạn đang không tỉnh táo,\nhãy đến bàn nước để pha cafe!";
        }

        // Hiển thị hoặc Ẩn
        if (warningMsg != "")
        {
            myHeadTextInstance.SetActive(true);
            myHeadTextContent.text = warningMsg;
            // Đổi màu chữ cho nguy hiểm (Đỏ)
            myHeadTextContent.color = Color.red; 
        }
        else
        {
            // Nếu trên 30% hết thì ẩn đi
            // (Lưu ý: Nếu bạn dùng chung prefab này cho việc "đau bụng", logic đó cần gọi riêng)
            myHeadTextInstance.SetActive(false); 
        }
    }

    void DecreaseStats()
    {
        // Tận dụng hàm Restore (truyền số âm)
        RestoreEnergy(-decayRate * Time.deltaTime);
        RestoreSanity(-decayRate * Time.deltaTime);
    }

    void UpdateUI()
    {
        // Cập nhật Slider
        if (energySlider != null) energySlider.value = currentEnergy / maxEnergy;
        if (sanitySlider != null) sanitySlider.value = currentSanity / maxSanity;

        // [MỚI] Cập nhật Text %
        if (energyPercentText != null) 
            energyPercentText.text = $"{Mathf.RoundToInt(currentEnergy)}%";
        
        if (sanityPercentText != null) 
            sanityPercentText.text = $"{Mathf.RoundToInt(currentSanity)}%";

        // Cập nhật thanh Code
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(CODE_PROGRESS_KEY))
        {
            float progress = (float)PhotonNetwork.CurrentRoom.CustomProperties[CODE_PROGRESS_KEY];
            if (codeSlider != null) codeSlider.value = progress / 100f;
        }
    }

    void FindSlidersAndTexts()
    {
        GameObject canvas = GameObject.Find("Canvas_HUD");
        if (canvas != null)
        {
            // Tìm Slider
            Slider[] allSliders = canvas.GetComponentsInChildren<Slider>();
            foreach(Slider s in allSliders)
            {
                if (s.name == "Slider_Code") codeSlider = s;
                if (s.name == "Slider_Energy") energySlider = s;
                if (s.name == "Slider_Sanity") sanitySlider = s;
            }

            // [MỚI] Tìm Text % (Bạn cần tạo 2 cái TextMeshProUGUI trong HUD và đặt tên như dưới)
            TextMeshProUGUI[] allTexts = canvas.GetComponentsInChildren<TextMeshProUGUI>();
            foreach(var t in allTexts)
            {
                if (t.name == "Text_Percent_Energy") energyPercentText = t;
                if (t.name == "Text_Percent_Sanity") sanityPercentText = t;
            }
        }
    }

    public void AddCodeProgress(float amount)
    {
        if (PhotonNetwork.IsMasterClient || !PhotonNetwork.IsMasterClient) 
        {
            float currentProg = 0;
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(CODE_PROGRESS_KEY))
                currentProg = (float)PhotonNetwork.CurrentRoom.CustomProperties[CODE_PROGRESS_KEY];
            
            currentProg += amount;
            Hashtable props = new Hashtable { { CODE_PROGRESS_KEY, currentProg } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            
            if (AudioManager.Instance != null) {
                 if (currentProg < 25) AudioManager.Instance.PlayStageMusic(0);
                 else if (currentProg < 50) AudioManager.Instance.PlayStageMusic(1);
                 else if (currentProg < 75) AudioManager.Instance.PlayStageMusic(2);
                 else AudioManager.Instance.PlayStageMusic(3);
            }
        }
    }
    
    public void RestoreEnergy(float amount)
    {
        currentEnergy += amount;
        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);
    }

     public void RestoreSanity(float amount)
    {
        currentSanity += amount;
        currentSanity = Mathf.Clamp(currentSanity, 0, maxSanity);
    }
}