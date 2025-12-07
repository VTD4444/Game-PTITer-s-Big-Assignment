using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class MinigameMechanical : MonoBehaviourPunCallbacks
{
    public static MinigameMechanical Instance;

    [Header("UI References")]
    public Transform inventoryContainer;
    public Transform[] machines;
    public Button btnTestRun;
    public TextMeshProUGUI textFeedback;
    public TextMeshProUGUI textAttempts;

    [Header("Tutorial")]
    public GameObject tutorialPanel;
    public TextMeshProUGUI textTutorialRole;

    [Header("Assets")]
    public GameObject[] corePrefabs;

    // --- TRẠNG THÁI GAME ---
    private MechDraggable[] allCoresUI = new MechDraggable[16];
    private Transform[,] machineSlots = new Transform[4, 4];
    private int[,] hiddenPressures = new int[4, 4];

    // --- BIẾN ĐỒNG BỘ ---
    private int operatorActorNumber = -1;
    private int inspectorActorNumber = -1;

    // --- KEY CHO ROOM PROPERTIES ---
    private const string KEY_OP_ID = "Mech_Op_ID";
    private const string KEY_INS_ID = "Mech_Ins_ID";

    private const int MAX_ATTEMPTS = 6;
    private int currentAttempts = 0;
    private bool isGameActive = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            GeneratePuzzle();
        }
    }

    void OnEnable()
    {
        MapSlots();
        if (allCoresUI[0] == null) SpawnCores();

        // Hiện Tutorial
        isGameActive = false;
        if (tutorialPanel != null) tutorialPanel.SetActive(true);
        
        // Reset vai trò tạm thời khi mới mở bảng
        operatorActorNumber = -1;
        inspectorActorNumber = -1;
        
        // Cập nhật dữ liệu từ Server về
        SyncRolesFromNetwork();

        // Bắt đầu quy trình xin vai trò
        CheckAndAssignRole();
    }

    void SyncRolesFromNetwork()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(KEY_OP_ID))
            operatorActorNumber = (int)PhotonNetwork.CurrentRoom.CustomProperties[KEY_OP_ID];
        
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(KEY_INS_ID))
            inspectorActorNumber = (int)PhotonNetwork.CurrentRoom.CustomProperties[KEY_INS_ID];
    }

    // --- LOGIC PHÂN VAI (FIX LỖI) ---
    void CheckAndAssignRole()
    {
        int myID = PhotonNetwork.LocalPlayer.ActorNumber;

        // 1. Nếu trên mạng đã ghi nhận tôi có vai trò rồi -> Cập nhật UI ngay
        if (myID == operatorActorNumber || myID == inspectorActorNumber)
        {
            UpdateRoleUI();
            return;
        }

        // 2. Nếu chưa ai làm Kỹ sư -> TÔI NHẬN LUÔN (Chiếm chỗ trước, báo cáo sau)
        if (operatorActorNumber == -1)
        {
            // Gán luôn cục bộ để UI hiện ngay lập tức
            operatorActorNumber = myID; 
            UpdateRoleUI(); 

            // Gửi lên mạng để báo cho người kia biết
            Hashtable props = new Hashtable { { KEY_OP_ID, myID } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
        // 3. Nếu đã có Kỹ sư rồi mà chưa có Kiểm định -> Tôi làm Kiểm định
        else if (inspectorActorNumber == -1)
        {
            inspectorActorNumber = myID;
            UpdateRoleUI();

            Hashtable props = new Hashtable { { KEY_INS_ID, myID } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
        else
        {
            UpdateRoleUI(); // Phòng đã đầy
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        // Khi có tin từ Server về, cập nhật lại cho chắc chắn
        if (propertiesThatChanged.ContainsKey(KEY_OP_ID))
            operatorActorNumber = (int)propertiesThatChanged[KEY_OP_ID];

        if (propertiesThatChanged.ContainsKey(KEY_INS_ID))
            inspectorActorNumber = (int)propertiesThatChanged[KEY_INS_ID];

        UpdateRoleUI();
    }

    void UpdateRoleUI()
    {
        int myID = PhotonNetwork.LocalPlayer.ActorNumber;

        if (btnTestRun) btnTestRun.gameObject.SetActive(false);
        SetCoresInteractable(false);
        if (textFeedback) textFeedback.text = "";

        string roleDesc = "";

        if (myID == operatorActorNumber)
        {
            roleDesc = "<color=yellow>VAI TRÒ: KỸ SƯ LẮP ĐẶT</color>";
            SetCoresInteractable(true);
        }
        else if (myID == inspectorActorNumber)
        {
            roleDesc = "<color=green>VAI TRÒ: KIỂM ĐỊNH VIÊN</color>";
            if (btnTestRun) btnTestRun.gameObject.SetActive(true);
        }
        else
        {
            roleDesc = "ĐANG CHỜ...\n(Người khác đang thao tác)";
        }

        if (textTutorialRole) textTutorialRole.text = roleDesc;
    }

    // --- CÁC HÀM LOGIC GAME (GIỮ NGUYÊN) ---
    public void StartActualGame()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        isGameActive = true;
    }

    void SetCoresInteractable(bool state)
    {
        foreach (var core in allCoresUI)
        {
            if (core != null)
            {
                CanvasGroup cg = core.GetComponent<CanvasGroup>();
                if (cg) cg.blocksRaycasts = state;
            }
        }
    }

    void MapSlots()
    {
        for (int m = 0; m < 4; m++)
        {
            if (machines[m] == null) continue;
            for (int s = 0; s < 4; s++)
            {
                Transform slot = machines[m].Find("Slot_" + s);
                if (slot != null)
                {
                    machineSlots[m, s] = slot;
                    MechSlot script = slot.GetComponent<MechSlot>();
                    if (script == null) script = slot.gameObject.AddComponent<MechSlot>();
                    script.machineID = m;
                    script.slotID = s;
                    script.isInventory = false;
                }
            }
        }
        if (inventoryContainer != null)
        {
            MechSlot invScript = inventoryContainer.GetComponent<MechSlot>();
            if (invScript == null) invScript = inventoryContainer.gameObject.AddComponent<MechSlot>();
            invScript.isInventory = true;
        }
    }

    void SpawnCores()
    {
        if (corePrefabs == null || corePrefabs.Length < 3) return;
        int[] levels = { 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3 };

        for (int i = 0; i < 16; i++)
        {
            GameObject prefabToUse = corePrefabs[levels[i] - 1];
            GameObject go = Instantiate(prefabToUse, inventoryContainer);
            if (go.GetComponent<CanvasGroup>() == null) go.AddComponent<CanvasGroup>();
            MechDraggable dr = go.GetComponent<MechDraggable>();
            if (dr == null) dr = go.AddComponent<MechDraggable>();
            dr.coreID = i;
            dr.level = levels[i];
            allCoresUI[i] = dr;
        }
    }

    void GeneratePuzzle()
    {
        List<int> pressures = new List<int> { 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3 };
        for (int i = 0; i < pressures.Count; i++)
        {
            int temp = pressures[i];
            int r = Random.Range(i, pressures.Count);
            pressures[i] = pressures[r];
            pressures[r] = temp;
        }
        int[] flatArray = pressures.ToArray();
        photonView.RPC("SyncPuzzleData", RpcTarget.AllBuffered, (object)flatArray);
    }

    [PunRPC]
    void SyncPuzzleData(int[] flatPressures)
    {
        int index = 0;
        for (int m = 0; m < 4; m++)
        {
            for (int s = 0; s < 4; s++)
            {
                hiddenPressures[m, s] = flatPressures[index];
                index++;
            }
        }
    }

    public void RequestMoveCore(int coreID, int machineID, int slotID, bool toInventory)
    {
        if (!isGameActive) return;
        if (PhotonNetwork.LocalPlayer.ActorNumber != operatorActorNumber) return;
        photonView.RPC("MoveCoreRPC", RpcTarget.All, coreID, machineID, slotID, toInventory);
    }

    Transform GetTargetParent(int machineID, int slotID, bool toInventory)
    {
        if (toInventory) return inventoryContainer;
        if (machineID >= 0 && machineID < 4 && slotID >= 0 && slotID < 4) return machineSlots[machineID, slotID];
        return null;
    }

    [PunRPC]
    void MoveCoreRPC(int coreID, int machineID, int slotID, bool toInventory)
    {
        if (coreID < 0 || coreID >= allCoresUI.Length) return;
        MechDraggable coreToMove = allCoresUI[coreID];
        if (coreToMove == null) return;

        Transform targetParent = GetTargetParent(machineID, slotID, toInventory);

        if (!toInventory && targetParent != null && targetParent.childCount > 0)
        {
            Transform existingCoreTrans = targetParent.GetChild(0);
            if (existingCoreTrans != null)
            {
                existingCoreTrans.SetParent(inventoryContainer);
                RectTransform existRT = existingCoreTrans.GetComponent<RectTransform>();
                if (existRT) existRT.anchoredPosition = Vector2.zero;
            }
        }

        if (targetParent != null)
        {
            coreToMove.transform.SetParent(targetParent);
            coreToMove.parentAfterDrag = targetParent;
            RectTransform rt = coreToMove.GetComponent<RectTransform>();
            if (rt)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
            }
            coreToMove.transform.localScale = Vector3.one;
        }
        ResetSlotColors();
    }

    void ResetSlotColors()
    {
        for (int m = 0; m < 4; m++)
        {
            for (int s = 0; s < 4; s++)
            {
                if(machineSlots[m,s] != null && machineSlots[m,s].GetComponent<Image>())
                    machineSlots[m,s].GetComponent<Image>().color = Color.white;
            }
        }
    }

    public void TryRunTestAll()
    {
        if (!isGameActive) return;
        if (PhotonNetwork.LocalPlayer.ActorNumber != inspectorActorNumber) return;

        if (!CheckAllSlotsFilled())
        {
            if (textFeedback) textFeedback.text = "CHƯA LẮP ĐỦ 16 LÕI!";
            return;
        }
        photonView.RPC("RunTestAllRPC", RpcTarget.All);
    }

    bool CheckAllSlotsFilled()
    {
        if (inventoryContainer.childCount > 0) return false;
        return true;
    }

    [PunRPC]
    void RunTestAllRPC()
    {
        currentAttempts++;
        if (textAttempts) textAttempts.text = $"LẦN THỬ: {currentAttempts}/{MAX_ATTEMPTS}";
        StartCoroutine(ShowResultSequence());
    }

    IEnumerator ShowResultSequence()
    {
        bool isAllPass = true;
        bool amIInspector = (PhotonNetwork.LocalPlayer.ActorNumber == inspectorActorNumber);

        for (int m = 0; m < 4; m++)
        {
            for (int s = 0; s < 4; s++)
            {
                Transform slotTrans = machineSlots[m, s];
                MechDraggable coreInSlot = slotTrans.GetComponentInChildren<MechDraggable>();
                if (coreInSlot == null) { isAllPass = false; continue; }

                bool slotPass = (coreInSlot.level >= hiddenPressures[m, s]);
                if (!slotPass) isAllPass = false;

                if (amIInspector)
                {
                    Image slotImage = slotTrans.GetComponent<Image>();
                    if (slotImage) slotImage.color = slotPass ? Color.green : Color.red;
                }
            }
        }

        if (!amIInspector)
        {
            if (textFeedback) textFeedback.text = "ĐANG CHỜ KẾT QUẢ TỪ KIỂM ĐỊNH VIÊN...";
        }

        yield return new WaitForSeconds(2.0f);
        ResetSlotColors();

        if (isAllPass)
        {
            if (textFeedback) textFeedback.text = "THÀNH CÔNG! (+25%)";
            if (PhotonNetwork.IsMasterClient && PlayerController.LocalPlayerInstance != null)
            {
                PlayerStats stats = PlayerController.LocalPlayerInstance.GetComponent<PlayerStats>();
                if (stats) stats.AddCodeProgress(25f);
            }
            Invoke("CloseGame", 2f);
        }
        else
        {
            if (currentAttempts >= MAX_ATTEMPTS)
            {
                if (textFeedback) textFeedback.text = "QUÁ TẢI! RESET TOÀN BỘ!";
                Invoke("ResetGameRPC", 3f);
            }
            else
            {
                if (textFeedback) textFeedback.text = "THẤT BẠI! HÃY THỬ LẠI.";
            }
        }
    }

    [PunRPC]
    void ResetGameRPC()
    {
        CloseGame();

        if (PhotonNetwork.IsMasterClient)
        {
            // Xóa role cũ
            Hashtable props = new Hashtable();
            props.Add(KEY_OP_ID, -1);
            props.Add(KEY_INS_ID, -1);
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);

            photonView.RPC("ResetAttemptsRPC", RpcTarget.AllBuffered);
            photonView.RPC("ReturnAllCoresToInventoryRPC", RpcTarget.All);
            GeneratePuzzle();
        }
    }

    [PunRPC]
    void ResetAttemptsRPC()
    {
        currentAttempts = 0;
        if(textAttempts) textAttempts.text = $"LẦN THỬ: 0/{MAX_ATTEMPTS}";
    }

    [PunRPC]
    void ReturnAllCoresToInventoryRPC()
    {
        foreach (var core in allCoresUI)
        {
            if (core != null)
            {
                core.transform.SetParent(inventoryContainer);
                RectTransform rt = core.GetComponent<RectTransform>();
                if (rt) rt.anchoredPosition = Vector2.zero;
            }
        }
    }
    
    void CloseGame()
    {
        InteractableObject[] interactables = FindObjectsOfType<InteractableObject>();
        foreach (var obj in interactables)
        {
            if (obj.type == InteractionType.Code) obj.CloseAllMinigames();
        }
    }
}