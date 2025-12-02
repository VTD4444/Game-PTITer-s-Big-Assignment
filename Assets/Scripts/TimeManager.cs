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
    public int startHour = 20;    // Bắt đầu lúc 20:00
    public int endHour = 7;       // Kết thúc lúc 7:00 (sáng hôm sau)
    public float realSecondsPerGameHour = 180f; // 3 phút ngoài đời = 1 giờ trong game (180 giây)

    private double startTime;
    private bool isTimerRunning = false;
    private const string START_TIME_KEY = "StartTime";

    // Biến lưu giờ hiện tại để các script khác (như ánh sáng/sự kiện) truy cập
    public float CurrentGameHour { get; private set; } 

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Chỉ Master Client mới có quyền bắt đầu đếm giờ
        if (PhotonNetwork.IsMasterClient)
        {
            SetStartTime();
        }
        else
        {
            // Client khác thì kiểm tra xem giờ đã chạy chưa để đồng bộ
            CheckStartTime();
        }
    }

    void Update()
    {
        if (!isTimerRunning) return;

        CalculateTime();
    }

    // --- ĐỒNG BỘ THỜI GIAN QUA MẠNG ---

    void SetStartTime()
    {
        // Lấy thời gian hiện tại của Server Photon
        startTime = PhotonNetwork.Time;

        // Lưu mốc thời gian bắt đầu vào Room Properties để ai vào sau cũng biết
        Hashtable props = new Hashtable { { START_TIME_KEY, startTime } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        
        isTimerRunning = true;
    }

    void CheckStartTime()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(START_TIME_KEY))
        {
            startTime = (double)PhotonNetwork.CurrentRoom.CustomProperties[START_TIME_KEY];
            isTimerRunning = true;
        }
    }

    // Tự động nhận cập nhật nếu có ai đó join sau hoặc reconnect
    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(START_TIME_KEY))
        {
            startTime = (double)propertiesThatChanged[START_TIME_KEY];
            isTimerRunning = true;
        }
    }

    // --- TÍNH TOÁN LOGIC ---

    void CalculateTime()
    {
        // 1. Tính số giây thực tế đã trôi qua kể từ lúc bắt đầu game
        double timeElapsed = PhotonNetwork.Time - startTime;

        // 2. Quy đổi ra giờ trong game
        // Ví dụ: Trôi qua 90s thực tế -> 0.5 giờ game
        float gameHoursPassed = (float)timeElapsed / realSecondsPerGameHour;

        // 3. Cộng với giờ bắt đầu (20h)
        float totalCurrentHour = startHour + gameHoursPassed;

        // Cập nhật biến Global để dùng cho việc đổi màu trời sau này
        CurrentGameHour = totalCurrentHour;

        // 4. Xử lý hiển thị (Format 24h)
        // Nếu vượt quá 24h (ví dụ 25h nghĩa là 1h sáng hôm sau)
        float displayHour = totalCurrentHour % 24; 
        
        int hours = (int)displayHour;
        int minutes = (int)((displayHour - hours) * 60);

        // Hiển thị lên UI (Dạng 00:00)
        clockText.text = string.Format("{0:00}:{1:00}", hours, minutes);

        // 5. Kiểm tra điều kiện kết thúc (07:00 sáng hôm sau)
        // 20h đến 7h sáng hôm sau tức là tổng cộng 11 tiếng trôi qua
        // 20 + 11 = 31. Vậy nếu totalCurrentHour >= 31 là hết giờ.
        if (totalCurrentHour >= (startHour + 11)) // 11 là số tiếng từ 20h->7h
        {
            EndGame();
        }
    }

    void EndGame()
    {
        isTimerRunning = false;
        clockText.text = "07:00";
        Debug.Log("TRỜI ĐÃ SÁNG! NỘP BÀI THÔI!");
    }
}