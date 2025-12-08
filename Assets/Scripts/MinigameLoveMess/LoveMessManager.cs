using UnityEngine;

public class LoveMessManager : MonoBehaviour
{
    public static LoveMessManager Instance;

    [Header("Trạng thái")]
    public bool IsEventActive = false; // Biến để InteractableObject kiểm tra xem có tin nhắn không
    public bool IsRinging = false;

    [Header("Visuals")]
    public GameObject notificationIcon; // Dấu chấm than (!) hoặc icon tin nhắn trên đầu điện thoại
    public AudioSource phoneRingSource; // Âm thanh chuông reo
    
    [Header("Cấu hình Phạt")]
    public float sanityDecayRate = 2.0f; // Mất 2 Sanity mỗi giây nếu lề mề

    void Awake()
    {
        // Singleton pattern để gọi được từ script khác
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        if (notificationIcon) notificationIcon.SetActive(false);
    }
    
    void Update()
    {
        // [YÊU CẦU 2] Tụt Sanity liên tục khi chuông đang reo
        // Chỉ trừ Sanity nếu đang Reo VÀ Game chưa kết thúc
        if (IsRinging)
        {
            if (PlayerStats.LocalInstance != null)
            {
                // Trừ Sanity của người chơi hiện tại
                PlayerStats.LocalInstance.RestoreSanity(-sanityDecayRate * Time.deltaTime);
            }
        }
    }

    // Hàm này được TimeManager gọi khi đến giờ
    public void TriggerEvent()
    {
        IsEventActive = true;
        IsRinging = true; // Bắt đầu reo và trừ điểm
        
        // Bật hiệu ứng thông báo để người chơi biết chạy lại bấm E
        if (notificationIcon) notificationIcon.SetActive(true);
        
        // Bật chuông reo
        if (phoneRingSource) 
        {
            phoneRingSource.loop = true; 
            phoneRingSource.Play();
        }
        
        Debug.Log("Điện thoại đang rung! Hãy ra bấm E để trả lời.");
    }
    
    // 2. GỌI KHI HOST MỞ ĐIỆN THOẠI (InteractableObject gọi)
    public void PickupPhone()
    {
        IsRinging = false; // Ngừng trừ điểm
        
        // Tắt tiếng chuông và icon
        if (phoneRingSource) 
        {
            phoneRingSource.Stop();
            phoneRingSource.loop = false;
        }
        if (notificationIcon) notificationIcon.SetActive(false);

        Debug.Log("Đã nghe máy. Ngừng tụt Sanity.");
    }

    // Hàm này được gọi khi Minigame kết thúc (Win/Fail/Tắt máy)
    public void EndEvent()
    {
        IsEventActive = false;
        IsRinging = false;
        
        // Tắt thông báo
        if (notificationIcon) notificationIcon.SetActive(false);
        if (phoneRingSource) phoneRingSource.Stop();
    }
}