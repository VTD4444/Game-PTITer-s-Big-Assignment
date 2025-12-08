using UnityEngine;
using Photon.Voice.Unity;
using Photon.Pun; // Dùng để kiểm tra IsMine

public class CharacterVoiceControl : MonoBehaviour
{
    private Recorder myRecorder;
    private PhotonView myPhotonView;

    void Start()
    {
        myRecorder = GetComponent<Recorder>();
        myPhotonView = GetComponent<PhotonView>();

        // Chỉ điều khiển Mic nếu đây là nhân vật của chính mình
        if (myPhotonView.IsMine)
        {
            // Mặc định bật mic khi vào game
            SetMic(true);
        }
    }

    void Update()
    {
        // Chỉ xử lý input nếu là nhân vật của mình
        if (!myPhotonView.IsMine) return;

        // Nhấn phím M để bật/tắt
        if (Input.GetKeyDown(KeyCode.V))
        {
            ToggleMic();
        }
    }

    public void ToggleMic()
    {
        if (myRecorder != null)
        {
            // Đảo ngược trạng thái hiện tại
            SetMic(!myRecorder.TransmitEnabled);
        }
    }

    public void SetMic(bool isActive)
    {
        if (myRecorder != null)
        {
            myRecorder.TransmitEnabled = isActive;
            Debug.Log("Mic status: " + (isActive ? "ON" : "OFF"));
        }
    }
}