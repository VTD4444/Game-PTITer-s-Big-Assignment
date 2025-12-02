using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using System.Collections.Generic;

public class MinigameFlow : MonoBehaviour
{
    [Header("UI References")]
    public Transform arrowContainer;
    public GameObject arrowPrefab;
    public Slider rhythmSlider;
    public TextMeshProUGUI feedbackText;
    public Slider timerSlider;
    public GameObject tutorialPanel;

    [Header("Config")]
    public float sliderSpeed = 2f;
    public int sequenceLength = 7;
    public float timeLimit = 2.0f;

    // Trạng thái
    private List<KeyCode> currentSequence = new List<KeyCode>();
    private List<GameObject> arrowObjs = new List<GameObject>();
    private bool isWaitingForSpace = false;
    private float sliderValue = 0f;
    private int sliderDirection = 1;
    private float remainingTime;
    
    // Biến kiểm soát trạng thái game
    private bool isGameActive = false; 

    public Sprite iconUp, iconDown, iconLeft, iconRight;

    void OnEnable()
    {
        // 1. Reset trạng thái UI
        if(feedbackText) feedbackText.text = "";
        if(rhythmSlider) rhythmSlider.value = 0;
        if(timerSlider) timerSlider.value = 1;
        
        // Xóa các mũi tên cũ nếu còn sót
        foreach (GameObject obj in arrowObjs) Destroy(obj);
        arrowObjs.Clear();

        // 2. Hiện Hướng dẫn trước
        isGameActive = false;
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }
        else
        {
            StartActualGame();
        }
    }

    // Hàm gọi từ nút Bắt Đầu
    public void StartActualGame()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        isGameActive = true;
        GenerateSequence(); // Lúc này mới bắt đầu sinh mũi tên và tính giờ
    }

    void Update()
    {
        // Nếu đang hiện hướng dẫn thì dừng mọi logic
        if (!isGameActive) return;

        HandleRhythmBar();

        if (!isWaitingForSpace)
        {
            // Trừ giờ
            remainingTime -= Time.deltaTime;
            if(timerSlider != null) timerSlider.value = remainingTime / timeLimit;

            if (remainingTime <= 0)
            {
                HandleFailure("TOO SLOW! (-1%)");
                return;
            }

            CheckArrowInput();
        }
        else
        {
            CheckSpaceInput();
        }
    }

    void GenerateSequence()
    {
        remainingTime = timeLimit;
        if(timerSlider != null) timerSlider.value = 1;
        isWaitingForSpace = false;

        foreach (GameObject obj in arrowObjs) Destroy(obj);
        arrowObjs.Clear();
        currentSequence.Clear();

        for (int i = 0; i < sequenceLength; i++)
        {
            int rand = Random.Range(0, 4);
            KeyCode key = KeyCode.None;
            Sprite icon = null;

            switch (rand)
            {
                case 0: key = KeyCode.UpArrow; icon = iconUp; break;
                case 1: key = KeyCode.DownArrow; icon = iconDown; break;
                case 2: key = KeyCode.LeftArrow; icon = iconLeft; break;
                case 3: key = KeyCode.RightArrow; icon = iconRight; break;
            }

            currentSequence.Add(key);

            GameObject arrow = Instantiate(arrowPrefab, arrowContainer);
            if(arrow.GetComponent<Image>()) arrow.GetComponent<Image>().sprite = icon;
            arrowObjs.Add(arrow);
        }
    }

    void CheckArrowInput()
    {
        if (currentSequence.Count == 0)
        {
            isWaitingForSpace = true;
            return;
        }

        KeyCode expectedKey = currentSequence[0];
        KeyCode pressedKey = KeyCode.None;

        if (Input.GetKeyDown(KeyCode.UpArrow)) pressedKey = KeyCode.UpArrow;
        else if (Input.GetKeyDown(KeyCode.DownArrow)) pressedKey = KeyCode.DownArrow;
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) pressedKey = KeyCode.LeftArrow;
        else if (Input.GetKeyDown(KeyCode.RightArrow)) pressedKey = KeyCode.RightArrow;

        if (pressedKey != KeyCode.None)
        {
            if (pressedKey == expectedKey)
            {
                currentSequence.RemoveAt(0);
                Destroy(arrowObjs[0]);
                arrowObjs.RemoveAt(0);
            }
            else
            {
                HandleFailure("WRONG KEY! (-1%)");
            }
        }
    }

    void HandleRhythmBar()
    {
        sliderValue += Time.deltaTime * sliderSpeed * sliderDirection;
        if (sliderValue >= 1f) { sliderValue = 1f; sliderDirection = -1; }
        else if (sliderValue <= 0f) { sliderValue = 0f; sliderDirection = 1; }
        
        if (rhythmSlider != null) rhythmSlider.value = sliderValue;
    }

    void CheckSpaceInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            float val = rhythmSlider.value;
            // 0.7 đến 0.75
            if (val >= 0.7f && val <= 0.75f)
            {
                ShowFeedback("PERFECT!! (+3%)", Color.cyan);
                AddProgress(3f);
                if(PlayerController.LocalPlayerInstance)
                    PlayerController.LocalPlayerInstance.GetComponent<PlayerStats>().RestoreSanity(5f);
            }
            else
            {
                ShowFeedback("GOOD (+1%)", Color.green);
                AddProgress(1f);
            }
            GenerateSequence();
        }
    }

    void HandleFailure(string reason)
    {
        ShowFeedback(reason, Color.red);
        AddProgress(-1f);
        GenerateSequence();
    }

    void AddProgress(float amount)
    {
        if (PlayerController.LocalPlayerInstance != null)
        {
            PlayerStats stats = PlayerController.LocalPlayerInstance.GetComponent<PlayerStats>();
            if (stats != null) stats.AddCodeProgress(amount);
        }
        CheckPhase2Win();
    }

    void CheckPhase2Win()
    {
         float currentProg = 0;
        if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("CodeProgress"))
            currentProg = (float)PhotonNetwork.CurrentRoom.CustomProperties["CodeProgress"];

        if (currentProg >= 50f)
        {
            if(feedbackText) feedbackText.text = "PHASE 2 COMPLETED!";
            Invoke("CloseGame", 2f);
        }
    }
    
    void CloseGame()
    {
        gameObject.SetActive(false);
        if (PlayerController.LocalPlayerInstance != null) 
            PlayerController.LocalPlayerInstance.canMove = true;
    }

    void ShowFeedback(string msg, Color color)
    {
        if(feedbackText)
        {
            feedbackText.text = msg;
            feedbackText.color = color;
        }
        CancelInvoke("ClearFeedback");
        Invoke("ClearFeedback", 1f);
    }
    void ClearFeedback() { if(feedbackText) feedbackText.text = ""; }
}