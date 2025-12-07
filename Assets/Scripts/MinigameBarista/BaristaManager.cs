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

        GenerateRecipe();
        StartCoroutine(GameRoutine());
    }

    void GenerateRecipe()
    {
        targetSequence.Clear();
        string recipeText = "CÔNG THỨC (Cho đúng theo thứ tự):\n";

        // 1. Random Nước (200 - 700ml) -> Mỗi lần kéo là 100ml
        int waterCount = Random.Range(2, 8); 
        recipeText += $"- {waterCount * 100}ml Nước\n";
        for(int i=0; i<waterCount; i++) targetSequence.Add(BaristaIngredient.Water);

        // 2. Random Cafe (1-2 gói)
        int coffeeCount = Random.Range(1, 3);
        recipeText += $"- {coffeeCount} gói Cafe\n";
        for(int i=0; i<coffeeCount; i++) targetSequence.Add(BaristaIngredient.Coffee);

        // 3. Random Đường (1-5 thìa)
        int sugarCount = Random.Range(1, 6);
        recipeText += $"- {sugarCount} thìa Đường\n";
        for(int i=0; i<sugarCount; i++) targetSequence.Add(BaristaIngredient.Sugar);

        // 4. Random Đá (1-10 viên)
        int iceCount = Random.Range(1, 11);
        recipeText += $"- {iceCount} viên Đá";
        for(int i=0; i<iceCount; i++) targetSequence.Add(BaristaIngredient.Ice);

        textRecipeDisplay.text = recipeText;
    }

    IEnumerator GameRoutine()
    {
        // Reset
        playerInput.Clear();
        if(textFeedback) textFeedback.text = "";

        // Giai đoạn 1: Hiện công thức
        panelRecipeView.SetActive(true);
        panelGameplay.SetActive(false);
        
        yield return new WaitForSeconds(3f); // Hiện 3 giây

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