using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("PANELS")]
    public GameObject mainPanel;
    public GameObject createPanel;
    public GameObject joinPanel;

    [Header("INPUTS")]
    public TMP_InputField createInput;
    public TMP_InputField joinInput;
    public TextMeshProUGUI statusText;

    void Start()
    {
        SetInteractable(false);
        ShowMainPanel(); // Mặc định hiện Main Panel lúc đầu

        if (!PhotonNetwork.IsConnected)
        {
            statusText.text = "Đang kết nối Server...";
            PhotonNetwork.ConnectUsingSettings();
        }
        else
        {
             PhotonNetwork.JoinLobby();
        }
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        // SỬA LỖI Ở ĐÂY:
        // Chỉ hiện thông báo "Sẵn sàng" nếu đang ở màn hình chính.
        // Nếu đang ở màn hình Nhập mã (Create/Join), giữ nguyên thông báo lỗi cũ để người chơi đọc.
        if (mainPanel.activeSelf) 
        {
            statusText.text = "Đã vào sảnh chờ! Sẵn sàng.";
        }
        
        SetInteractable(true);
    }

    // --- CÁC HÀM UI ---
    public void ShowMainPanel()
    {
        mainPanel.SetActive(true);
        createPanel.SetActive(false);
        joinPanel.SetActive(false);
        if(PhotonNetwork.InLobby) statusText.text = "Đã vào sảnh chờ! Sẵn sàng.";
    }

    public void ShowCreatePanel()
    {
        mainPanel.SetActive(false);
        createPanel.SetActive(true);
        joinPanel.SetActive(false);
        statusText.text = "";
    }

    public void ShowJoinPanel()
    {
        mainPanel.SetActive(false);
        createPanel.SetActive(false);
        joinPanel.SetActive(true);
        statusText.text = "";
    }

    // --- LOGIC MẠNG ---
    public void CreateRoom()
    {
        if (!PhotonNetwork.IsConnectedAndReady || !PhotonNetwork.InLobby) return;
        if (string.IsNullOrEmpty(createInput.text)) return;

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 2;
        PhotonNetwork.CreateRoom(createInput.text, roomOptions);
        statusText.text = "Đang tạo phòng...";
        SetInteractable(false);
    }

    public void JoinRoom()
    {
        if (!PhotonNetwork.IsConnectedAndReady) return;

        // Nếu bị mất kết nối Lobby, tự kết nối lại
        if (!PhotonNetwork.InLobby) 
        {
            PhotonNetwork.JoinLobby();
            return;
        }

        if (string.IsNullOrEmpty(joinInput.text)) return;

        PhotonNetwork.JoinRoom(joinInput.text);
        statusText.text = "Đang tìm phòng...";
        SetInteractable(false);
    }

    public override void OnJoinedRoom()
    {
        statusText.text = "Thành công!";
        PhotonNetwork.AutomaticallySyncScene = true;
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("CharacterSelection");
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        // 32758 = Game does not exist
        if (returnCode == 32758) statusText.text = "Phòng chưa được tạo!";
        else statusText.text = "Lỗi: " + message;

        SetInteractable(true);
        
        // Đảm bảo vẫn ở trong Lobby để thử lại
        if (!PhotonNetwork.InLobby) PhotonNetwork.JoinLobby();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        statusText.text = "Tạo thất bại: " + message;
        SetInteractable(true);
    }

    void SetInteractable(bool state)
    {
        // Chỉ khóa nút bấm, KHÔNG ẩn Panel đang hiện
        // Điều này giúp giữ nguyên màn hình Join khi bị lỗi
        if (createInput) createInput.interactable = state;
        if (joinInput) joinInput.interactable = state;
    }
}