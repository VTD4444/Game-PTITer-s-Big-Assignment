using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using System.Collections;
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
    public float stunDuration = 3.0f; // Thời gian bị đóng băng (3s)

    // Trạng thái
    private List<KeyCode> currentSequence = new List<KeyCode>();
    private List<GameObject> arrowObjs = new List<GameObject>();
    private bool isWaitingForSpace = false;
    private float sliderValue = 0f;
    private int sliderDirection = 1;
    private float remainingTime;
    
    // Biến kiểm soát trạng thái game
    private bool isGameActive = false; 
    private bool isStunned = false; // Biến kiểm tra đang bị choáng

    public Sprite iconUp, iconDown, iconLeft, iconRight;

    void OnEnable()
    {
        // Reset toàn bộ trạng thái
        if(feedbackText) feedbackText.text = "";
        if(rhythmSlider) rhythmSlider.value = 0;
        if(timerSlider) timerSlider.value = 1;
        
        foreach (GameObject obj in arrowObjs) Destroy(obj);
        arrowObjs.Clear();

        isGameActive = false;
        isStunned = false; // Reset choáng

        // Hiện Hướng dẫn
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }
        else
        {
            StartActualGame();
        }
    }

    public void StartActualGame()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        isGameActive = true;
        isStunned = false;
        GenerateSequence();
    }

    void Update()
    {
        // Nếu đang hiện hướng dẫn HOẶC ĐANG BỊ CHOÁNG thì dừng mọi logic
        if (!isGameActive || isStunned) return;

        HandleRhythmBar();

        if (!isWaitingForSpace)
        {
            // Trừ giờ
            remainingTime -= Time.deltaTime;
            if(timerSlider != null) timerSlider.value = remainingTime / timeLimit;

            // Hết giờ -> Bị phạt và Choáng
            if (remainingTime <= 0)
            {
                StartCoroutine(StunSequence("QUÁ CHẬM! (-1%)"));
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
                // Bấm sai -> Bị phạt và Choáng
                StartCoroutine(StunSequence("SAI PHÍM! (-1%)"));
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
            // Perfect: 0.7 - 0.75
            if (val >= 0.7f && val <= 0.75f)
            {
                ShowFeedback("TUYỆT VỜI!! (+3%)", Color.cyan);
                AddProgress(3f);
                if(PlayerController.LocalPlayerInstance)
                    PlayerController.LocalPlayerInstance.GetComponent<PlayerStats>().RestoreSanity(5f);
            }
            else
            {
                ShowFeedback("KHÁ TỐT (+1%)", Color.green);
                AddProgress(1f);
            }
            GenerateSequence();
        }
    }

    // --- LOGIC CHOÁNG (STUN) MỚI ---
    IEnumerator StunSequence(string reason)
    {
        isStunned = true; // Bật cờ choáng -> Update sẽ dừng xử lý input
        
        // Trừ điểm ngay lập tức
        AddProgress(-1f);
        
        // Xóa sạch các mũi tên hiện tại để màn hình trống trơn (nhìn cho sợ)
        foreach (GameObject obj in arrowObjs) Destroy(obj);
        arrowObjs.Clear();
        currentSequence.Clear();

        // Đếm ngược 3 giây
        float stunTimer = stunDuration;
        while (stunTimer > 0)
        {
            if(feedbackText)
            {
                feedbackText.color = Color.red;
                feedbackText.text = $"{reason}\nĐÓNG BĂNG: {stunTimer:0}s";
            }
            yield return new WaitForSeconds(1f);
            stunTimer--;
        }

        // Hết giờ choáng
        if(feedbackText) feedbackText.text = "";
        isStunned = false; // Mở khóa
        
        GenerateSequence(); // Sinh chuỗi mới để chơi tiếp
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