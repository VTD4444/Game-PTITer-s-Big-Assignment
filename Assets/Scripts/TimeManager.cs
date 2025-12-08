using UnityEngine;
using TMPro;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class TimeManager : MonoBehaviourPunCallbacks
{
    public static TimeManager Instance;

    [Header("UI Reference")]
    public TextMeshProUGUI clockText;

    [Header("Cấu hình Thời gian")]
    public int startHour = 20;    // 20:00
    public int endHour = 7;       // 07:00 sáng hôm sau
    public float realSecondsPerGameHour = 180f; // 180s = 1 giờ game

    private double startTime;
    private bool isTimerRunning = false;
    
    // --- KEYS (Thêm key mới cho Love Message) ---
    private const string START_TIME_KEY = "StartTime";
    private const string WIFI_TIME_2_KEY = "WifiTime2"; 
    private const string WIFI_TIME_4_KEY = "WifiTime4"; 
    private const string LOVE_MESS_TIME_KEY = "LoveMessTime"; // [NEW] Key đồng bộ

    // --- BIẾN SỰ KIỆN ---
    private float wifiBreakTimePhase2 = -1f;
    private float wifiBreakTimePhase4 = -1f;
    private float loveMessTime = -1f; // [NEW] Thời gian xảy ra tin nhắn

    private bool hasTriggeredPhase2 = false;
    private bool hasTriggeredPhase4 = false;
    private bool hasTriggeredLoveMess = false; // [NEW] Cờ kiểm tra đã kích hoạt chưa

    public float CurrentGameHour { get; private set; } 

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            SetStartTimeAndEvents();
        }
        else
        {
            CheckStartTimeAndEvents();
        }
    }

    void Update()
    {
        if (!isTimerRunning) return;

        CalculateTime();
        
        // Chỉ Master Client mới được quyền kích hoạt sự kiện để tránh spam lệnh
        if (PhotonNetwork.IsMasterClient)
        {
            CheckEvents();
        }
    }

    // --- ĐỒNG BỘ THỜI GIAN & SỰ KIỆN ---

    void SetStartTimeAndEvents()
    {
        startTime = PhotonNetwork.Time;

        // 1. Wifi Phase 2 (23h - 2h sáng) -> 23.0 đến 26.0
        float rndPhase2 = Random.Range(23.0f, 26.0f);
        
        // 2. Wifi Phase 4 (5h - 7h sáng) -> 29.0 đến 30.5
        float rndPhase4 = Random.Range(29.0f, 30.5f);

        // 3. [NEW] Love Message (0h - 2h sáng) -> 24.0 đến 26.0
        // Lưu ý: 20h bắt đầu -> 24h là mốc 24.0, 2h sáng là mốc 26.0
        float rndLoveMess = Random.Range(24.0f, 26.0f);
        // rndLoveMess = startHour + 0.02f;

        Hashtable props = new Hashtable
        {
            { START_TIME_KEY, startTime },
            { WIFI_TIME_2_KEY, rndPhase2 },
            { WIFI_TIME_4_KEY, rndPhase4 },
            { LOVE_MESS_TIME_KEY, rndLoveMess } // [NEW]
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        
        // Cập nhật biến cục bộ luôn cho Master
        wifiBreakTimePhase2 = rndPhase2;
        wifiBreakTimePhase4 = rndPhase4;
        loveMessTime = rndLoveMess; // [NEW]

        isTimerRunning = true;
        
        Debug.Log($"[TimeManager] Event Schedule: Wifi2({FormatHour(rndPhase2)}), LoveMess({FormatHour(rndLoveMess)}), Wifi4({FormatHour(rndPhase4)})");
    }

    void CheckStartTimeAndEvents()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(START_TIME_KEY, out object timeVal))
        {
            startTime = (double)timeVal;
            isTimerRunning = true;
        }
        
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(WIFI_TIME_2_KEY, out object val2))
            wifiBreakTimePhase2 = (float)val2;
            
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(WIFI_TIME_4_KEY, out object val4))
            wifiBreakTimePhase4 = (float)val4;

        // [NEW] Nhận thời gian Love Message
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(LOVE_MESS_TIME_KEY, out object valLove))
            loveMessTime = (float)valLove;
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(START_TIME_KEY))
        {
            startTime = (double)propertiesThatChanged[START_TIME_KEY];
            isTimerRunning = true;
        }
        
        if (propertiesThatChanged.ContainsKey(WIFI_TIME_2_KEY))
            wifiBreakTimePhase2 = (float)propertiesThatChanged[WIFI_TIME_2_KEY];
            
        if (propertiesThatChanged.ContainsKey(WIFI_TIME_4_KEY))
            wifiBreakTimePhase4 = (float)propertiesThatChanged[WIFI_TIME_4_KEY];

        // [NEW] Cập nhật khi có thay đổi (ví dụ rejoin phòng)
        if (propertiesThatChanged.ContainsKey(LOVE_MESS_TIME_KEY))
            loveMessTime = (float)propertiesThatChanged[LOVE_MESS_TIME_KEY];
    }

    // --- LOGIC ---

    void CalculateTime()
    {
        double timeElapsed = PhotonNetwork.Time - startTime;
        float gameHoursPassed = (float)timeElapsed / realSecondsPerGameHour;
        float totalCurrentHour = startHour + gameHoursPassed;

        CurrentGameHour = totalCurrentHour;

        // Hiển thị
        clockText.text = FormatHour(totalCurrentHour);

        if (totalCurrentHour >= 31f) 
        {
            EndGame();
        }
    }

    string FormatHour(float totalHour)
    {
        float displayHour = totalHour % 24;
        int hours = (int)displayHour;
        int minutes = (int)((displayHour - hours) * 60);
        return string.Format("{0:00}:{1:00}", hours, minutes);
    }

    void CheckEvents()
    {
        // 1. Kiểm tra Wifi Phase 2
        if (!hasTriggeredPhase2 && wifiBreakTimePhase2 > 0 && CurrentGameHour >= wifiBreakTimePhase2)
        {
            hasTriggeredPhase2 = true;
            Debug.Log("EVENT: WIFI PHASE 2!");
            if (WifiManager.Instance) WifiManager.Instance.TriggerWifiBreak();
        }

        // 2. [NEW] Kiểm tra Love Message (0h-2h)
        if (!hasTriggeredLoveMess && loveMessTime > 0 && CurrentGameHour >= loveMessTime)
        {
            hasTriggeredLoveMess = true;
            Debug.Log("EVENT: LOVE MESSAGE!");
            
            // Gọi RPC để bật UI cho tất cả mọi người (hoặc xử lý logic hiển thị)
            photonView.RPC("RpcTriggerLoveMessage", RpcTarget.All);
        }

        // 3. Kiểm tra Wifi Phase 4
        if (!hasTriggeredPhase4 && wifiBreakTimePhase4 > 0 && CurrentGameHour >= wifiBreakTimePhase4)
        {
            hasTriggeredPhase4 = true;
            Debug.Log("EVENT: WIFI PHASE 4!");
            if (WifiManager.Instance) WifiManager.Instance.TriggerWifiBreak();
        }
    }

    // [NEW] Hàm RPC để bật Minigame Love Message trên máy tất cả người chơi
    [PunRPC]
    void RpcTriggerLoveMessage()
    {
        // SỬA ĐỔI Ở ĐÂY: Gọi LoveMessManager thay vì MinigameLoveMess
        if (LoveMessManager.Instance != null)
        {
            Debug.Log("TimeManager: Đã gọi LoveMessManager bật sự kiện!");
            LoveMessManager.Instance.TriggerEvent(); 
        }
        else
        {
            Debug.LogWarning("Không tìm thấy LoveMessManager.Instance! Hãy kiểm tra xem đã tạo GameObject LoveMessManager trong scene chưa.");
        }
    }

    void EndGame()
    {
        isTimerRunning = false;
        clockText.text = "07:00";
        Debug.Log("HẾT GIỜ!");
    }
}