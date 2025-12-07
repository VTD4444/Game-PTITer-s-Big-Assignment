using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class MinigameDecode : MonoBehaviourPunCallbacks
{
    [Header("UI Containers")]
    public GameObject gameUIContainer;
    public GameObject failPanel;
    
    [Header("UI References")]
    public Transform historyContainer; // Nơi chứa các dòng kết quả (Chỉ Analyst thấy)
    public GameObject rowPrefab;       // Prefab dòng lịch sử
    public GameObject keypadPanel;     // Bảng phím số (Chỉ Hacker thấy)
    public GameObject tutorialPanel;   // Panel Hướng dẫn
    public TextMeshProUGUI textTutorialRole; // Text báo vai trò trong tutorial

    [Header("Input Display (TEXT)")]
    public TextMeshProUGUI[] inputSlots;
    
    [Header("Assets")]
    public Sprite[] numberSprites; // Kéo 10 ảnh số (0-9) vào đây. Thứ tự phải chuẩn!
    public Sprite emptySprite;     // Ảnh rỗng (khi chưa nhập gì)

    [Header("Stats")]
    public TextMeshProUGUI textFeedback;
    public TextMeshProUGUI textFragments;
    public TextMeshProUGUI textLives;

    // --- LOGIC ---
    private int[] targetCode = new int[4];
    private List<int> currentInput = new List<int>();
    private int fragmentsFound = 0;
    private int currentLives = 4;

    // --- PHÂN VAI ---
    private int hackerID = -1;
    private int analystID = -1;
    private const string KEY_DEC_HACKER = "Dec_Hacker";
    private const string KEY_DEC_ANALYST = "Dec_Analyst";
    
    private bool isGameActive = false;

    void OnEnable()
    {
        if (!isGameActive)
        {
            if(tutorialPanel) tutorialPanel.SetActive(true);
            if(textTutorialRole) textTutorialRole.text = "ĐANG ĐĂNG KÝ VAI TRÒ...";
        }
        else
        {
            // Nếu đang chơi dở, đảm bảo UI container bật lên
            if(gameUIContainer) gameUIContainer.SetActive(true);
        }

        if (failPanel) failPanel.SetActive(false);
        
        currentInput.Clear();
        UpdateInputDisplay();
        UpdateStats();

        hackerID = -1; analystID = -1;
        SyncRoles();
        AssignRole();

        // Chỉ sinh mã mới nếu LẦN ĐẦU TIÊN vào game (mảng targetCode chưa có dữ liệu)
        if (PhotonNetwork.IsMasterClient && (targetCode == null || targetCode.Length == 0 || targetCode[0] == 0)) 
        {
            GenerateNewCode();
        }
    }

    void SyncRoles()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(KEY_DEC_HACKER))
            hackerID = (int)PhotonNetwork.CurrentRoom.CustomProperties[KEY_DEC_HACKER];
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(KEY_DEC_ANALYST))
            analystID = (int)PhotonNetwork.CurrentRoom.CustomProperties[KEY_DEC_ANALYST];
    }

    void AssignRole()
    {
        int myID = PhotonNetwork.LocalPlayer.ActorNumber;
        if (myID == hackerID || myID == analystID) { UpdateUIByRole(); return; }

        if (hackerID == -1)
        {
            hackerID = myID;
            UpdateUIByRole();
            Hashtable props = new Hashtable { { KEY_DEC_HACKER, myID } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
        else if (analystID == -1)
        {
            analystID = myID;
            UpdateUIByRole();
            Hashtable props = new Hashtable { { KEY_DEC_ANALYST, myID } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
        else UpdateUIByRole();
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(KEY_DEC_HACKER)) hackerID = (int)propertiesThatChanged[KEY_DEC_HACKER];
        if (propertiesThatChanged.ContainsKey(KEY_DEC_ANALYST)) analystID = (int)propertiesThatChanged[KEY_DEC_ANALYST];
        UpdateUIByRole();
    }

    void UpdateUIByRole()
    {
        int myID = PhotonNetwork.LocalPlayer.ActorNumber;
        
        // Mặc định ẩn hết
        keypadPanel.SetActive(false);
        historyContainer.gameObject.SetActive(false); 

        string roleTxt = "";

        if (myID == hackerID)
        {
            roleTxt = "<color=green>HACKER (NHẬP MÃ)</color>";
            keypadPanel.SetActive(true); // Hacker thấy bàn phím
        }
        else if (myID == analystID)
        {
            roleTxt = "<color=yellow>ANALYST (PHÂN TÍCH)</color>";
            historyContainer.gameObject.SetActive(true); // Analyst thấy lịch sử
        }
        else roleTxt = "ĐANG CHỜ...";

        if(textTutorialRole) textTutorialRole.text = roleTxt;
    }

    public void StartGame() { if(tutorialPanel) tutorialPanel.SetActive(false); isGameActive = true; }

    // --- INPUT LOGIC ---
    public void OnNumberPress(int number)
    {
        if (!isGameActive) return;
        // Chỉ Hacker được nhập
        if (PhotonNetwork.LocalPlayer.ActorNumber != hackerID) return;

        if (currentInput.Count < 4)
        {
            currentInput.Add(number);
            photonView.RPC("SyncInputRPC", RpcTarget.All, currentInput.ToArray());
            
            // Tự động kiểm tra khi đủ 4 số
            if (currentInput.Count == 4)
            {
                photonView.RPC("SubmitGuessRPC", RpcTarget.All, currentInput.ToArray());
            }
        }
    }

    [PunRPC]
    void SyncInputRPC(int[] data)
    {
        currentInput = new List<int>(data);
        UpdateInputDisplay();
    }

    void UpdateInputDisplay()
    {
        if(inputSlots == null) return;
        for (int i = 0; i < 4; i++)
        {
            if(i >= inputSlots.Length) break;
            
            // --- THAY ĐỔI: Dùng Text ---
            if (i < currentInput.Count)
                inputSlots[i].text = currentInput[i].ToString();
            else 
                inputSlots[i].text = "_";
        }
    }

    // --- GAME LOGIC ---
    void GenerateNewCode()
    {
        List<int> digits = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        for (int i = 0; i < 4; i++) {
            int r = Random.Range(0, digits.Count);
            targetCode[i] = digits[r];
            digits.RemoveAt(r);
        }
        photonView.RPC("SyncCodeRPC", RpcTarget.All, targetCode);
    }

    [PunRPC] void SyncCodeRPC(int[] c) { targetCode = c; currentLives = 4; UpdateStats(); }

    [PunRPC]
    void SubmitGuessRPC(int[] guess)
    {
        if(rowPrefab && historyContainer)
        {
            GameObject row = Instantiate(rowPrefab, historyContainer);
            row.transform.SetAsLastSibling();

            int correct = 0;
            for (int i = 0; i < 4; i++)
            {
                int num = guess[i];
                if(i < row.transform.childCount)
                {
                    Transform slot = row.transform.GetChild(i);
                    Image bg = slot.GetComponent<Image>();
                    
                    // --- THAY ĐỔI: Tìm Text con để điền số ---
                    TextMeshProUGUI numText = slot.GetComponentInChildren<TextMeshProUGUI>();
                    if(numText) numText.text = num.ToString();

                    // Tô màu Background
                    bool isCorrectNum = false;
                    foreach(int t in targetCode) if(t == num) isCorrectNum = true;

                    if (num == targetCode[i]) { bg.color = Color.cyan; correct++; } 
                    else if (isCorrectNum) bg.color = Color.yellow; 
                    else bg.color = Color.gray;
                }
            }

            currentInput.Clear();
            UpdateInputDisplay();

            if (correct == 4)
            {
                fragmentsFound++; UpdateStats();
                
                // --- THÔNG BÁO HOÀN THÀNH MÃ ---
                if (fragmentsFound >= 10)
                {
                    ShowFeedback("GIẢI MÃ HOÀN TẤT!", Color.green);
                    WinGame();
                }
                else 
                {
                    ShowFeedback($"MÃ CHÍNH XÁC! ({fragmentsFound}/10)", Color.green);
                    if (PhotonNetwork.IsMasterClient) GenerateNewCode();
                }
            }
            else
            {
                currentLives--; UpdateStats();
                if (currentLives <= 0) 
                {
                    ShowFeedback("THẤT BẠI! HỆ THỐNG ĐANG KHÓA...", Color.red);
                    StartCoroutine(FailSequence());
                }
                else
                {
                    ShowFeedback("MÃ SAI! THỬ LẠI.", Color.red);
                }
            }
        }
    }
    
    // Hàm hiển thị thông báo chung
    void ShowFeedback(string msg, Color color)
    {
        if(textFeedback)
        {
            textFeedback.text = msg;
            textFeedback.color = color;
            // Tự xóa sau 2 giây nếu không phải thông báo thắng/thua lớn
            if(isGameActive) Invoke("ClearFeedback", 2f);
        }
    }
    void ClearFeedback() { if(textFeedback) textFeedback.text = ""; }
    
    IEnumerator FailSequence()
    {
        isGameActive = false; // Khóa input ngay lập tức

        // 1. Ẩn toàn bộ UI chơi game
        if(gameUIContainer) gameUIContainer.SetActive(false);

        // 2. Hiện thông báo phạt
        if(failPanel) failPanel.SetActive(true);

        // 3. Đếm ngược 10 giây (Có thể update text đếm ngược nếu muốn)
        yield return new WaitForSeconds(10f);

        // 4. Khôi phục lại
        if(failPanel) failPanel.SetActive(false);
        if(gameUIContainer) gameUIContainer.SetActive(true);
        
        // 5. Reset mã mới (chỉ Master làm rồi sync)
        if(PhotonNetwork.IsMasterClient) GenerateNewCode();
        
        // Clear lịch sử cũ để bắt đầu vòng mới sạch sẽ
        if(historyContainer) 
            foreach (Transform child in historyContainer) Destroy(child.gameObject);

        isGameActive = true; // Mở lại input
    }

    void UpdateStats()
    {
        if(textFragments) textFragments.text = $"MÃ ĐÃ GIẢI: {fragmentsFound}/10";
        if(textLives) textLives.text = $"LƯỢT THỬ: {currentLives}/4";
    }

    void WinGame()
    {
        if(textTutorialRole) textTutorialRole.text = "NHIỆM VỤ HOÀN THÀNH!";
        if(tutorialPanel) tutorialPanel.SetActive(true); // Hiện bảng báo thắng
        // Cộng điểm cuối cùng
        if (PhotonNetwork.IsMasterClient) PlayerController.LocalPlayerInstance.GetComponent<PlayerStats>().AddCodeProgress(25f);
        Invoke("CloseGame", 3f);
    }
    
    void CloseGame() { 
        foreach(var obj in FindObjectsOfType<InteractableObject>()) 
            if(obj.type == InteractionType.Code) obj.CloseAllMinigames(); 
    }
}