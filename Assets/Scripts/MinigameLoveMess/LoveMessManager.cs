using UnityEngine;
using Photon.Pun;

public class LoveMessManager : MonoBehaviourPun
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
    
    [Header("Cảnh báo")]
    public GameObject playerHeadTextPrefab; // Kéo Prefab "Player Head Text" vào
    private GameObject currentHeadText;     // Lưu tham chiếu để xóa sau này

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
        // LOGIC TRỪ ĐIỂM & HIỆN TEXT CẢNH BÁO
        if (IsRinging)
        {
            // 1. Trừ Sanity
            if (PlayerStats.LocalInstance != null)
            {
                PlayerStats.LocalInstance.RestoreSanity(-sanityDecayRate * Time.deltaTime);
                
                // 2. Hiện Text cảnh báo trên đầu (nếu chưa có)
                if (currentHeadText == null && playerHeadTextPrefab != null)
                {
                    ShowWarningText();
                }
            }
        }
        else
        {
            // Nếu hết reo thì xóa text đi cho đỡ rối mắt
            if (currentHeadText != null) Destroy(currentHeadText);
        }
        
        // --- 2. [FIX LỖI ICON] CƠ CHẾ TỰ ĐỒNG BỘ VISUAL ---
        // Đảm bảo Icon luôn bật/tắt đúng theo biến IsRinging
        if (notificationIcon != null)
        {
            // Nếu trạng thái Icon khác với trạng thái Reo -> Ép lại cho đúng
            if (notificationIcon.activeSelf != IsRinging)
            {
                notificationIcon.SetActive(IsRinging);
            }
        }
        
        // Đồng bộ cả âm thanh (chống lỗi mất tiếng hoặc reo mãi không tắt)
        if (phoneRingSource != null)
        {
            if (IsRinging && !phoneRingSource.isPlaying) 
            {
                phoneRingSource.loop = true;
                phoneRingSource.Play();
            }
            else if (!IsRinging && phoneRingSource.isPlaying)
            {
                phoneRingSource.Stop();
            }
        }
    }
    
    void ShowWarningText()
    {
        if (PlayerController.LocalPlayerInstance != null)
        {
            currentHeadText = Instantiate(playerHeadTextPrefab, PlayerController.LocalPlayerInstance.transform);
            currentHeadText.transform.localPosition = new Vector3(0, 1.5f, 0); // Chỉnh cao độ
            
            // Tìm TextMeshPro để gán nội dung
            var tmp = currentHeadText.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (tmp) 
            {
                tmp.text = "Lại gần điện thoại ở trên giường để nghe máy!";
            }
        }
    }

    // --- 1. KÍCH HOẠT SỰ KIỆN ---
    public void TriggerEvent()
    {
        IsEventActive = true;
        
        // Gọi RPC để tất cả cùng reo
        if (photonView != null) photonView.RPC("RpcStartRinging", RpcTarget.AllBuffered);
        else RpcStartRinging();
    }
    
    [PunRPC]
    void RpcStartRinging()
    {
        IsRinging = true;
        if (notificationIcon) notificationIcon.SetActive(true);
        if (phoneRingSource) { phoneRingSource.loop = true; phoneRingSource.Play(); }
    }
    
    // --- 2. NGHE MÁY (TẠM DỪNG CHUÔNG) ---
    public void PickupPhone()
    {
        if (photonView != null) photonView.RPC("RpcStopRinging", RpcTarget.AllBuffered);
        else RpcStopRinging();
    }

    [PunRPC]
    void RpcStopRinging()
    {
        IsRinging = false; // Tạm ngừng trừ điểm
        if (phoneRingSource) phoneRingSource.Stop();
        // if (notificationIcon) notificationIcon.SetActive(false);
        // Text cảnh báo sẽ tự mất trong Update
    }

    // --- 3. [MỚI] REO LẠI (NẾU TẮT PANEL MÀ CHƯA XONG) ---
    public void ResumeRinging()
    {
        // Chỉ reo lại nếu sự kiện vẫn còn (chưa EndEvent)
        if (IsEventActive)
        {
            if (photonView != null) photonView.RPC("RpcStartRinging", RpcTarget.AllBuffered);
            else RpcStartRinging();
            Debug.Log("Chưa trả lời xong đã tắt máy -> Reo tiếp!");
        }
    }

    // --- 4. KẾT THÚC HOÀN TOÀN ---
    public void EndEvent()
    {
        if (photonView != null) photonView.RPC("RpcEndEvent", RpcTarget.AllBuffered);
        else RpcEndEvent();
    }

    [PunRPC]
    void RpcEndEvent()
    {
        IsEventActive = false;
        IsRinging = false;
        if (notificationIcon) notificationIcon.SetActive(false);
        if (phoneRingSource) phoneRingSource.Stop();
        if (currentHeadText != null) Destroy(currentHeadText);
    }
}