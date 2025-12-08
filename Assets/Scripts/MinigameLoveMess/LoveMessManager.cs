using UnityEngine;

public class LoveMessManager : MonoBehaviour
{
    public static LoveMessManager Instance;

    [Header("Trạng thái")]
    public bool IsEventActive = false; // Biến để InteractableObject kiểm tra xem có tin nhắn không

    [Header("Visuals")]
    public GameObject notificationIcon; // Dấu chấm than (!) hoặc icon tin nhắn trên đầu điện thoại
    public AudioSource phoneRingSource; // Âm thanh chuông reo

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

    // Hàm này được TimeManager gọi khi đến giờ (0h-2h)
    public void TriggerEvent()
    {
        IsEventActive = true;
        
        // Bật hiệu ứng thông báo để người chơi biết chạy lại bấm E
        if (notificationIcon) notificationIcon.SetActive(true);
        
        // Bật chuông reo
        if (phoneRingSource) phoneRingSource.Play();
        
        Debug.Log("Điện thoại đang rung! Hãy ra bấm E để trả lời.");
    }

    // Hàm này được gọi khi Minigame kết thúc (Win/Fail/Tắt máy)
    public void EndEvent()
    {
        IsEventActive = false;
        
        // Tắt thông báo
        if (notificationIcon) notificationIcon.SetActive(false);
        if (phoneRingSource) phoneRingSource.Stop();
    }
}