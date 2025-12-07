using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public enum Ingredient 
{ 
    Mi,         // 0 - Bắt buộc
    ThitLon,    // 1
    DuiGa,      // 2
    Tom,        // 3
    DuaChuot,   // 4
    Ot,         // 5
    XucXich,    // 6
    SuHao,      // 7
    CaChua,     // 8
    Nam,        // 9
    Trung,      // 10
    SupLo,      // 11
    CaRot       // 12
}

public class CookingManager : MonoBehaviourPunCallbacks
{
    public static CookingManager Instance;

    [Header("UI Panels")]
    public GameObject panelFridge;
    public GameObject panelCooking;
    public GameObject panelSelection; // Panel chọn "Nấu" hay "Xem món"
    public GameObject panelRecipes;   // Panel hiện tên 2 món trong 3s

    [Header("Fridge UI")]
    public Transform basketContainer; // Nơi hiện đồ đã lấy trong tủ lạnh
    public GameObject ingredientIconPrefab; // Prefab icon để sinh ra trong giỏ
    
    [Header("Assets - Ingredients")]
    // --- MỚI: Kéo ảnh nguyên liệu vào đây theo đúng thứ tự Enum ---
    public Sprite[] ingredientSprites;

    [Header("Cooking UI")]
    public Transform playerInventoryContainer; // Nơi hiện đồ mang theo ở Bếp
    public Transform potContainer;             // Nơi hiện đồ đã bỏ vào nồi
    public TextMeshProUGUI textCookingStatus;
    public Slider cookingSlider;
    public Button btnEat; // Nút Ăn

    [Header("Recipe UI")]
    public TextMeshProUGUI textRecipe1;
    public TextMeshProUGUI textRecipe2;

    [Header("World UI")]
    public GameObject toiletWorldCanvas; // Canvas trên đầu Toilet (chứa Text + Slider)
    public Slider toiletSlider;
    public TextMeshProUGUI toiletText; // "Pẹt pẹt..."

    [Header("Player Status")]
    public GameObject playerHeadTextPrefab; // Prefab Text "Đau bụng" spawn trên đầu player
    private GameObject currentHeadText;
    
    [Header("Audio")]
    public AudioSource potAudioSource;

    // --- DATA ---
    // Lưu 2 công thức mục tiêu (mỗi công thức là 1 list nguyên liệu)
    private List<List<Ingredient>> targetRecipes = new List<List<Ingredient>>();
    private List<string> targetRecipeNames = new List<string>();

    // Đồ đang cầm trên người (Giỏ hàng)
    private List<Ingredient> playerInventory = new List<Ingredient>();
    
    // Đồ đang trong nồi
    private List<Ingredient> potIngredients = new List<Ingredient>();

    // Trạng thái
    private bool isCooking = false;
    private float cookTimer = 0f;
    public bool IsSick { get; private set; } = false; // Biến check đau bụng

    void Awake() { Instance = this; }

    void Start()
    {
        CloseAllPanels();
        // Ẩn UI Toilet lúc đầu
        if(toiletWorldCanvas) toiletWorldCanvas.SetActive(false);
    }

    void Update()
    {
        if(btnEat) btnEat.interactable = isCooking;
        // Logic đếm giờ nấu (Vẫn chạy dù đóng panel)
        if (isCooking)
        {
            cookTimer += Time.deltaTime;
            
            if (panelCooking.activeSelf)
            {
                if(cookingSlider) cookingSlider.value = cookTimer / 50f;
                if(textCookingStatus) textCookingStatus.text = $"ĐANG NẤU: {cookTimer:F1}s";
                
                // Gợi ý màu sắc thanh slider
                Image fill = cookingSlider.fillRect.GetComponent<Image>();
                if (fill)
                {
                    if (cookTimer < 30) fill.color = Color.yellow;      // Chưa chín
                    else if (cookTimer <= 40) fill.color = Color.green; // Chín tới (Perfect)
                    else fill.color = Color.red;                        // Cháy
                }
            }
            
            if (potAudioSource != null)
            {
                // Chỉ kêu khi ở vùng Perfect (30s - 40s)
                bool inPerfectZone = (cookTimer >= 30f && cookTimer <= 40f);
                
                if (inPerfectZone)
                {
                    if (!potAudioSource.isPlaying) potAudioSource.Play();
                }
                else
                {
                    if (potAudioSource.isPlaying) potAudioSource.Stop();
                }
            }
        }
        else
        {
            // Ngừng nấu thì tắt tiếng ngay
            if (potAudioSource != null && potAudioSource.isPlaying) 
                potAudioSource.Stop();
        }
    }

    // --- MỞ BẾP (GỌI TỪ INTERACTABLE OBJECT) ---
    public void OpenKitchenInteraction()
    {
        if (IsSick) return; // Đau bụng không làm gì được ngoài đi Toilet

        // Nếu đang nấu dở -> Mở thẳng vào màn hình nấu
        if (isCooking)
        {
            OpenCookingPanel();
        }
        else
        {
            // Nếu chưa nấu -> Mở màn hình lựa chọn
            panelSelection.SetActive(true);
            if(PlayerController.LocalPlayerInstance) PlayerController.LocalPlayerInstance.canMove = false;
        }
    }

    // --- LỰA CHỌN 1: XEM MÓN ĂN ---
    public void OnClickViewRecipes()
    {
        // Sinh 2 món mới (Nếu chưa có hoặc muốn reset)
        if(targetRecipes.Count == 0) GenerateTwoRecipes();
        
        panelSelection.SetActive(false);
        panelRecipes.SetActive(true);
        
        // Hiển thị
        textRecipe1.text = "MÓN 1:\n" + targetRecipeNames[0];
        textRecipe2.text = "MÓN 2:\n" + targetRecipeNames[1];

        StartCoroutine(HideRecipesAfterTime(3f));
    }

    IEnumerator HideRecipesAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
        panelRecipes.SetActive(false);
        if(PlayerController.LocalPlayerInstance) PlayerController.LocalPlayerInstance.canMove = true;
    }

    // --- LỰA CHỌN 2: NẤU ĂN ---
    public void OnClickStartCookingMode()
    {
        panelSelection.SetActive(false);
        OpenCookingPanel();
    }

    void OpenCookingPanel()
    {
        panelCooking.SetActive(true);
        if(PlayerController.LocalPlayerInstance) PlayerController.LocalPlayerInstance.canMove = false;

        // --- SỬA LỖI CRASH Ở ĐÂY ---
        // Nếu chưa có đơn hàng (do người chơi quên xem), tự động tạo mới ngay
        if (targetRecipes == null || targetRecipes.Count == 0)
        {
            GenerateTwoRecipes();
        }
        // ---------------------------

        RenderIngredients(playerInventory, playerInventoryContainer, false);
        // RenderIngredients(potIngredients, potContainer, false); // Đã bỏ theo yêu cầu cũ

        if (!isCooking)
        {
            potIngredients.Clear();
            foreach(Transform child in potContainer) Destroy(child.gameObject);
            cookTimer = 0;
            if(textCookingStatus) textCookingStatus.text = "KÉO NGUYÊN LIỆU VÀO NỒI";
            if(cookingSlider) cookingSlider.value = 0;
        }
    }

    // --- LOGIC KÉO THẢ VÀO NỒI ---
    public void AddToPot(Ingredient ing)
    {
        // Chỉ được thêm nếu chưa bấm nút Nấu (Hoặc game này vừa nấu vừa thêm? Giả sử vừa nấu vừa thêm cũng được)
        // Nhưng logic đúng là thêm xong mới bật bếp. Ở đây ta cho thêm thoải mái.
        
        // Xóa khỏi Inventory người chơi
        if (playerInventory.Contains(ing))
        {
            playerInventory.Remove(ing);
            potIngredients.Add(ing);
            
            // Re-render
            RenderIngredients(playerInventory, playerInventoryContainer, false);
            RenderIngredients(potIngredients, potContainer, false); // SlotType Pot không cho kéo đi
            
            // Tự động bật bếp khi có nguyên liệu đầu tiên
            if (!isCooking)
            {
                isCooking = true;
                cookTimer = 0f;
            }
        }
    }

    // --- LOGIC NÚT "ĂN" (FINISH COOKING) ---
    public void FinishCooking()
    {
        isCooking = false;
        
        // 1. Kiểm tra thời gian
        bool timePerfect = (cookTimer >= 30f && cookTimer <= 40f);
        
        // 2. Kiểm tra công thức
        // So sánh đồ trong nồi với Món 1 HOẶC Món 2
        bool recipeMatch = CheckRecipeMatch(potIngredients, targetRecipes[0]) || 
                           CheckRecipeMatch(potIngredients, targetRecipes[1]);
        
        AudioManager.Instance.ToggleBoilingSound(false); // Tắt tiếng sôi ngay
        AudioManager.Instance.PlayEat(); // TIẾNG ĂN

        CloseAllPanels();

        if (timePerfect && recipeMatch)
        {
            // THÀNH CÔNG
            AudioManager.Instance.PlayWin(); // TIẾNG THẮNG
            if(PlayerController.LocalPlayerInstance)
            {
                var stats = PlayerController.LocalPlayerInstance.GetComponent<PlayerStats>();
                stats.RestoreEnergy(50f);
                stats.RestoreSanity(10f);
            }
        }
        else
        {
            // THẤT BẠI -> ĐAU BỤNG
            AudioManager.Instance.PlayFail(); // TIẾNG ĐAU BỤNG/FAIL
            TriggerSickness();
        }

        // Reset nồi
        potIngredients.Clear();
    }

    bool CheckRecipeMatch(List<Ingredient> input, List<Ingredient> target)
    {
        if (input.Count != target.Count) return false;
        // Kiểm tra xem input có chứa tất cả target không (không quan tâm thứ tự)
        List<Ingredient> temp = new List<Ingredient>(input);
        foreach (var item in target)
        {
            if (temp.Contains(item)) temp.Remove(item);
            else return false;
        }
        return true;
    }

    // --- TỦ LẠNH ---
    public void OpenFridge()
    {
        if (IsSick) return;
        panelFridge.SetActive(true);
        if(PlayerController.LocalPlayerInstance) PlayerController.LocalPlayerInstance.canMove = false;
        // Mở tủ lạnh thì hiện những gì đang có trong giỏ
        RenderIngredients(playerInventory, basketContainer, false);
    }

    public void AddToBasket(Ingredient ing)
    {
        playerInventory.Add(ing);
        RenderIngredients(playerInventory, basketContainer, false);
    }

    // --- HÀM HỖ TRỢ RENDER UI ---
    void RenderIngredients(List<Ingredient> data, Transform container, bool isInfinite)
    {
        // Xóa cũ
        foreach (Transform child in container) Destroy(child.gameObject);

        foreach (var item in data)
        {
            GameObject go = Instantiate(ingredientIconPrefab, container);
            
            // 1. Hiển thị Tên
            TextMeshProUGUI txt = go.GetComponentInChildren<TextMeshProUGUI>();
            if(txt) txt.text = GetIngredientName(item);

            // 2. Hiển thị Ảnh (Sprite)
            Image img = go.GetComponent<Image>();
            int index = (int)item;
            if (img != null && ingredientSprites != null && index < ingredientSprites.Length)
            {
                img.sprite = ingredientSprites[index];
                // img.preserveAspect = true; // Giữ tỉ lệ ảnh nếu cần
            }

            // 3. Setup Script Drag
            var drag = go.GetComponent<CookingDraggable>();
            if(drag == null) drag = go.AddComponent<CookingDraggable>();
            drag.ingredientType = item;
            drag.isInfiniteSource = isInfinite;
        }
    }
    
    string GetIngredientName(Ingredient ing)
    {
        // Hiển thị tiếng Việt
        switch(ing) {
            case Ingredient.Mi: return "Mì";
            case Ingredient.ThitLon: return "Thịt Lợn";
            case Ingredient.DuiGa: return "Đùi Gà";
            case Ingredient.Tom: return "Tôm";
            case Ingredient.DuaChuot: return "Dưa Chuột";
            case Ingredient.Ot: return "Ớt";
            case Ingredient.XucXich: return "Xúc Xích";
            case Ingredient.SuHao: return "Su Hào";
            case Ingredient.CaChua: return "Cà Chua";
            case Ingredient.Nam: return "Nấm";
            case Ingredient.Trung: return "Trứng";
            case Ingredient.SupLo: return "Súp Lơ";
            case Ingredient.CaRot: return "Cà Rốt";
            default: return ing.ToString();
        }
    }

    // --- HỆ THỐNG ĐAU BỤNG & TOILET ---
    public void TriggerSickness()
    {
        IsSick = true;
        
        // Hiện Text trên đầu nhân vật
        if(PlayerController.LocalPlayerInstance)
        {
            if (currentHeadText) Destroy(currentHeadText);
            currentHeadText = Instantiate(playerHeadTextPrefab, PlayerController.LocalPlayerInstance.transform);
            currentHeadText.transform.localPosition = new Vector3(0, 1.5f, 0); // Cao hơn đầu chút
            currentHeadText.GetComponentInChildren<TextMeshProUGUI>().text = "Đau bụng...";
        }
    }

    public void OpenToiletInteraction()
    {
        if (!IsSick) return; // Khỏe mạnh thì không cho đi

        // Khóa di chuyển, hiện UI Toilet
        if(PlayerController.LocalPlayerInstance) PlayerController.LocalPlayerInstance.canMove = false;
        
        if(toiletWorldCanvas) toiletWorldCanvas.SetActive(true);
        StartCoroutine(ToiletRoutine());
    }

    IEnumerator ToiletRoutine()
    {
        float duration = 20f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            if(toiletSlider) toiletSlider.value = timer / duration;
            if(toiletText) toiletText.text = "Pẹt pẹt" + new string('.', (int)(timer % 4));
            yield return null;
        }

        // Xong
        IsSick = false;
        if(toiletWorldCanvas) toiletWorldCanvas.SetActive(false);
        if(currentHeadText) Destroy(currentHeadText); // Xóa chữ trên đầu
        if(PlayerController.LocalPlayerInstance) PlayerController.LocalPlayerInstance.canMove = true;
    }

    // --- HÀM SINH CÔNG THỨC (LOGIC GIỮ NGUYÊN) ---
    void GenerateTwoRecipes()
    {
        targetRecipes.Clear();
        targetRecipeNames.Clear();

        // Danh sách nguyên liệu phụ (Trừ Mì ra)
        List<Ingredient> sideDishes = new List<Ingredient>();
        for (int i = 1; i <= 12; i++) sideDishes.Add((Ingredient)i);

        for(int i=0; i<2; i++)
        {
            List<Ingredient> r = new List<Ingredient>();
            string name = "Mì";
            
            // 1. Luôn có Mì
            r.Add(Ingredient.Mi);

            // 2. Random 3 món phụ không trùng nhau
            List<Ingredient> pool = new List<Ingredient>(sideDishes);
            for(int j=0; j<3; j++)
            {
                if(pool.Count > 0)
                {
                    int idx = Random.Range(0, pool.Count);
                    Ingredient item = pool[idx];
                    r.Add(item);
                    name += ", " + GetIngredientName(item);
                    pool.RemoveAt(idx);
                }
            }
            
            targetRecipes.Add(r);
            targetRecipeNames.Add(name);
        }
    }

    public void CloseAllPanels()
    {
        if(panelFridge) panelFridge.SetActive(false);
        if(panelCooking) panelCooking.SetActive(false);
        if(panelSelection) panelSelection.SetActive(false);
        if(panelRecipes) panelRecipes.SetActive(false);
        
        if (!IsSick && PlayerController.LocalPlayerInstance)
            PlayerController.LocalPlayerInstance.canMove = true;
    }
}