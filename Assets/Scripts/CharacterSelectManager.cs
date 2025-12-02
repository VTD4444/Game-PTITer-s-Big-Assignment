using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using System.Collections.Generic; // Thêm thư viện để dùng List

public class CharacterSelectManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    // Thay vì khai báo btn1, btn2... ta dùng một Mảng (Array)
    public Button[] charButtons;       
    public Button btnStartGame;
    
    // Key lưu dữ liệu
    private const string PLAYER_SELECTION_PROP = "PlayerSelection";

    void Start()
    {
        btnStartGame.interactable = false;
        
        // Gán sự kiện bấm nút cho từng button bằng code (cho đỡ phải kéo thả tay 12 lần)
        for (int i = 0; i < charButtons.Length; i++)
        {
            int index = i; // Bắt buộc phải tạo biến tạm này để dùng trong lambda
            charButtons[i].onClick.AddListener(() => SelectCharacter(index));
        }

        UpdateUI();
    }

    // Hàm chọn tướng (Logic vẫn như cũ)
    public void SelectCharacter(int charIndex)
    {
        Hashtable props = new Hashtable();
        props.Add(PLAYER_SELECTION_PROP, charIndex);
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public void OnStartGameClick()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.LoadLevel("MainGame");
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) { UpdateUI(); }
    public override void OnPlayerEnteredRoom(Player newPlayer) { UpdateUI(); }
    public override void OnPlayerLeftRoom(Player otherPlayer) { UpdateUI(); }

    void UpdateUI()
    {
        // 1. Reset trạng thái: Mở khóa tất cả các nút trước
        for (int i = 0; i < charButtons.Length; i++)
        {
            charButtons[i].interactable = true;
            // Tìm component Text con để xóa tên người chơi cũ
            TextMeshProUGUI txt = charButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if(txt) txt.text = ""; 
        }

        int playersReady = 0;

        // 2. Duyệt qua danh sách người chơi để xem ai chọn gì
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.ContainsKey(PLAYER_SELECTION_PROP))
            {
                int selectedIndex = (int)p.CustomProperties[PLAYER_SELECTION_PROP];

                // Đảm bảo chỉ số nằm trong giới hạn 0-11
                if (selectedIndex >= 0 && selectedIndex < charButtons.Length)
                {
                    // Khóa nút này lại
                    charButtons[selectedIndex].interactable = false;
                    
                    // Hiện tên người chơi lên nút
                    TextMeshProUGUI txt = charButtons[selectedIndex].GetComponentInChildren<TextMeshProUGUI>();
                    if(txt) txt.text = p.NickName;

                    playersReady++;
                }
            }
        }

        // 3. Logic nút Start (Đủ 2 người chơi và cả 2 đã chọn)
        // Lưu ý: Nếu muốn test 1 mình thì sửa số 2 thành số 1
        if (PhotonNetwork.IsMasterClient && playersReady == PhotonNetwork.CurrentRoom.PlayerCount)
        {
            btnStartGame.interactable = true;
        }
        else
        {
            btnStartGame.interactable = false;
        }
    }
}