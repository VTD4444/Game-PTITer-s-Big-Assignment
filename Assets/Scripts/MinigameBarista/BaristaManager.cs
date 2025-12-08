using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public enum BaristaIngredient 
{ 
    Water, 
    Coffee, 
    Sugar, 
    Ice 
}

public class BaristaManager : MonoBehaviour
{
    public static BaristaManager Instance;

    [Header("UI References")]
    public GameObject panelMain;
    public GameObject panelTutorial;   // [MỚI] Panel hướng dẫn ban đầu
    public GameObject panelRecipeView;
    public GameObject panelGameplay;
    public TextMeshProUGUI textRecipeDisplay;
    public TextMeshProUGUI textFeedback; // Hiện kết quả

    // --- LOGIC ---
    // Danh sách các hành động cần làm (Ví dụ: Nước, Nước, Cafe, Đường...)
    private List<BaristaIngredient> targetSequence = new List<BaristaIngredient>();
    private List<BaristaIngredient> playerInput = new List<BaristaIngredient>();
    
    private bool isGameActive = false;

    void Awake() { Instance = this; }

    void Start()
    {
        if(panelMain) panelMain.SetActive(false);
    }

    public void OpenBaristaGame()
    {
        // Kiểm tra đau bụng
        if (CookingManager.Instance != null && CookingManager.Instance.IsSick) return;

        panelMain.SetActive(true);
        if(PlayerController.LocalPlayerInstance) PlayerController.LocalPlayerInstance.canMove = false;

        ShowTutorial();
    }
    
    void ShowTutorial()
    {
        panelTutorial.SetActive(true);
        panelRecipeView.SetActive(false);
        panelGameplay.SetActive(false);
        
        // Reset dữ liệu cũ
        if(textFeedback) textFeedback.text = "";
        playerInput.Clear();
    }

    // 2. Hàm này gắn vào nút "BẮT ĐẦU" ở Panel Tutorial
    public void OnClickStartGame()
    {
        panelTutorial.SetActive(false); // Tắt hướng dẫn
        GenerateRandomRecipe();         // Sinh công thức ngẫu nhiên
        StartCoroutine(RecipeMemorizeRoutine()); // Bắt đầu đếm ngược 5s nhớ công thức
    }

    // [MỚI] Hàm sinh công thức có random thứ tự
    void GenerateRandomRecipe()
    {
        targetSequence.Clear();
        string recipeText = "CÔNG THỨC (Làm đúng thứ tự):\n";

        // Tạo danh sách các loại nguyên liệu
        List<BaristaIngredient> availableTypes = new List<BaristaIngredient>() 
        { 
            BaristaIngredient.Water, 
            BaristaIngredient.Coffee, 
            BaristaIngredient.Sugar, 
            BaristaIngredient.Ice 
        };

        // --- Xáo trộn thứ tự (Shuffle) ---
        for (int i = 0; i < availableTypes.Count; i++)
        {
            BaristaIngredient temp = availableTypes[i];
            int randomIndex = Random.Range(i, availableTypes.Count);
            availableTypes[i] = availableTypes[randomIndex];
            availableTypes[randomIndex] = temp;
        }

        // --- Duyệt qua danh sách đã xáo trộn để tạo công thức ---
        int stepIndex = 1;
        foreach (BaristaIngredient type in availableTypes)
        {
            int count = 0;
            string stepName = "";

            switch (type)
            {
                case BaristaIngredient.Water:
                    count = Random.Range(2, 8); // 200-700ml
                    stepName = $"{count * 100}ml Nước";
                    break;
                case BaristaIngredient.Coffee:
                    count = Random.Range(1, 3); // 1-2 gói
                    stepName = $"{count} gói Cafe";
                    break;
                case BaristaIngredient.Sugar:
                    count = Random.Range(1, 6); // 1-5 thìa
                    stepName = $"{count} thìa Đường";
                    break;
                case BaristaIngredient.Ice:
                    count = Random.Range(1, 11); // 1-10 viên
                    stepName = $"{count} viên Đá";
                    break;
            }

            // Ghi vào text hiển thị
            recipeText += $"{stepIndex}. {stepName}\n";
            
            // Thêm vào logic game (add đúng số lượng yêu cầu)
            for(int k=0; k<count; k++) targetSequence.Add(type);
            
            stepIndex++;
        }

        textRecipeDisplay.text = recipeText;
    }
    
    IEnumerator RecipeMemorizeRoutine()
    {
        // Giai đoạn: Hiện công thức để nhớ
        panelRecipeView.SetActive(true);
        
        yield return new WaitForSeconds(5f); // Người chơi có 5 giây để nhớ

        // Giai đoạn: Pha chế
        panelRecipeView.SetActive(false);
        panelGameplay.SetActive(true);
        isGameActive = true;
    }

    IEnumerator GameRoutine()
    {
        // Reset
        playerInput.Clear();
        if(textFeedback) textFeedback.text = "";

        // Giai đoạn 1: Hiện công thức
        panelRecipeView.SetActive(true);
        panelGameplay.SetActive(false);
        
        yield return new WaitForSeconds(5f); // Hiện 5 giây

        // Giai đoạn 2: Pha chế
        panelRecipeView.SetActive(false);
        panelGameplay.SetActive(true);
        isGameActive = true;
    }

    // Gọi từ BaristaCup khi thả đồ vào
    public void AddIngredient(BaristaIngredient type)
    {
        if (!isGameActive) return;
        playerInput.Add(type);

        switch (type)
        {
            case BaristaIngredient.Water:
                AudioManager.Instance.PlayPourWater(); // TIẾNG RÓT NƯỚC
                break;
            case BaristaIngredient.Sugar:
                AudioManager.Instance.PlaySugar();
                break;
            default:
                AudioManager.Instance.PlayGetThings();
                break;
        }
    }

    // Gọi từ nút "PHA XONG"
    public void FinishMaking()
    {
        isGameActive = false;
        bool isCorrect = CheckResult();

        if (isCorrect)
        {
            if(textFeedback) textFeedback.text = "<color=green>PERFECT! THƠM NGON!</color>";
            if (PlayerController.LocalPlayerInstance)
            {
                var stats = PlayerController.LocalPlayerInstance.GetComponent<PlayerStats>();
                stats.RestoreSanity(50f); // Hồi 50% Sanity
            }
        }
        else
        {
            if(textFeedback) textFeedback.text = "<color=red>SAI CÔNG THỨC RỒI!\n(Đau bụng quá...)</color>";
            if (PlayerController.LocalPlayerInstance)
            {
                var stats = PlayerController.LocalPlayerInstance.GetComponent<PlayerStats>();
                stats.RestoreSanity(20f); // Hồi ít hơn
            }
            
            // Gọi hàm đau bụng bên CookingManager
            if(CookingManager.Instance) CookingManager.Instance.TriggerSickness();
        }

        StartCoroutine(CloseDelay());
    }

    bool CheckResult()
    {
        // 1. Kiểm tra số lượng tổng
        if (playerInput.Count != targetSequence.Count) return false;

        // 2. Kiểm tra đúng thứ tự từng món
        for (int i = 0; i < targetSequence.Count; i++)
        {
            if (playerInput[i] != targetSequence[i]) return false;
        }

        return true;
    }

    IEnumerator CloseDelay()
    {
        yield return new WaitForSeconds(2f);
        CloseAll();
    }

    public void CloseAll()
    {
        panelMain.SetActive(false);
        if (PlayerController.LocalPlayerInstance != null) 
        {
            PlayerController.LocalPlayerInstance.canMove = true;
        }
    }
}