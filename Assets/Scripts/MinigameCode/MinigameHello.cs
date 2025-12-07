using UnityEngine;
using TMPro;
using Photon.Pun;
using System.Collections.Generic;

public class MinigameHello : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI targetText;   // Text đề bài
    public TMP_InputField inputField;    // Ô nhập liệu
    public TextMeshProUGUI feedbackText; // Text báo đúng/sai
    public GameObject tutorialPanel;     // Kéo Panel_Tutorial_Hello vào đây

    [Header("Game Config")]
    public float winThreshold = 25f;

    // Trạng thái game (để chặn người chơi gõ khi đang xem hướng dẫn)
    private bool isGameActive = false;

    private string[] codeSnippets = new string[]
    {
        "cout << \"Hello World\";",
        "int a = 10;",
        "if (a > b) return;",
        "for (int i=0; i<10; i++)",
        "while (true) break;",
        "switch (option) {",
        "case 1: break;",
        "return 0;",
        "float x = 5.5f;",
        "void Update() { }",
        "#include <iostream>",
        "using namespace std;",
        "print(\"Hello World\");"
    };

    private string currentCode;

    // Chạy khi Panel chính được bật (Bấm E)
    void OnEnable()
    {
        // 1. Reset trạng thái
        isGameActive = false;
        if(feedbackText != null) feedbackText.text = "";
        if(inputField != null) inputField.text = "";

        // 2. Hiện bảng hướng dẫn lên trước
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }
        else
        {
            // Nếu quên gán tutorial panel thì vào game luôn
            StartActualGame();
        }
    }

    // Hàm này gắn vào nút "BẮT ĐẦU CODE"
    public void StartActualGame()
    {
        // 1. Ẩn hướng dẫn
        if (tutorialPanel != null) tutorialPanel.SetActive(false);

        // 2. Bắt đầu game
        isGameActive = true;
        SpawnNewCode();

        // 3. Focus vào ô nhập liệu ngay lập tức
        if (inputField != null)
        {
            inputField.ActivateInputField();
            inputField.Select();
        }
    }

    void Update()
    {
        // Nếu chưa bấm bắt đầu thì không làm gì cả
        if (!isGameActive) return;

        // Kiểm tra Enter
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (inputField != null && inputField.text.Length > 0)
            {
                CheckInput();
            }
        }

        // Giữ focus (để không phải click chuột lại)
        if (inputField != null && !inputField.isFocused)
        {
            inputField.ActivateInputField();
        }
    }

    void SpawnNewCode()
    {
        if (codeSnippets.Length > 0)
        {
            currentCode = codeSnippets[Random.Range(0, codeSnippets.Length)];
            if (targetText != null) targetText.text = currentCode;
        }
        if (inputField != null) inputField.text = "";
    }

    void CheckInput()
    {
        if (inputField.text.Trim() == currentCode)
        {
            // ĐÚNG
            ShowFeedback(true);
            
            if (PlayerController.LocalPlayerInstance != null)
            {
                PlayerStats stats = PlayerController.LocalPlayerInstance.GetComponent<PlayerStats>();
                if (stats != null)
                {
                    stats.AddCodeProgress(1f);
                    CheckWinCondition();
                }
            }
            SpawnNewCode();
        }
        else
        {
            // SAI
            ShowFeedback(false);
            
            if (PlayerController.LocalPlayerInstance != null)
            {
                PlayerStats stats = PlayerController.LocalPlayerInstance.GetComponent<PlayerStats>();
                if (stats != null) stats.AddCodeProgress(-2f);
            }
            inputField.text = "";
        }
    }

    void ShowFeedback(bool isCorrect)
    {
        if (feedbackText == null) return;

        if (isCorrect)
        {
            feedbackText.color = Color.green;
            feedbackText.text = "CORRECT! (+1%)";
        }
        else
        {
            feedbackText.color = Color.red;
            feedbackText.text = "SYNTAX ERROR! (-2%)";
        }

        CancelInvoke("HideFeedback");
        Invoke("HideFeedback", 1f);
    }

    void HideFeedback()
    {
        if (feedbackText != null) feedbackText.text = "";
    }

    void CheckWinCondition()
    {
        // Lấy tiến độ hiện tại
        float currentProg = 0;
        if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("CodeProgress"))
        {
            currentProg = (float)PhotonNetwork.CurrentRoom.CustomProperties["CodeProgress"];
        }

        // Đạt 25% -> Hoàn thành Phase 1
        if (currentProg >= winThreshold)
        {
            if(feedbackText != null)
            {
                feedbackText.color = Color.yellow;
                feedbackText.text = "PHASE 1 COMPLETED!";
            }
            if(targetText != null) targetText.text = "Đã HOÀN THÀNH GIAI ĐOẠN 1! ĐANG CHUYỂN GIAI ĐOẠN...";
            
            // Khóa không cho nhập nữa
            if(inputField != null) inputField.interactable = false;

            // Đợi 3 giây rồi đóng
            Invoke("CloseGame", 3f);
        }
    }

    void CloseGame()
    {
        gameObject.SetActive(false);
        if (PlayerController.LocalPlayerInstance != null)
        {
            PlayerController.LocalPlayerInstance.canMove = true;
        }
    }
}