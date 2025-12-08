using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections.Generic;
using TMPro;

public class WifiManager : MonoBehaviourPunCallbacks, IPunObservable
{
    public static WifiManager Instance;

    [Header("UI Panels")]
    public GameObject panelRouter; 
    public GameObject panelPC;     

    [Header("Router Controls (Player A)")]
    public RectTransform crosshair;
    public float moveSpeed = 500f; 

    [Header("PC View (Player B)")]
    public RectTransform ghostCrosshair; 
    public Transform targetContainer;
    public GameObject targetPrefab;
    public TextMeshProUGUI pcStatusText;

    [Header("Game Logic")]
    public float hitRadius = 50f; 
    // LƯU Ý: Chỉnh số này khớp với kích thước Panel Router trong Unity
    public Vector2 playAreaSize = new Vector2(800, 500); 

    // --- SYNC VARS ---
    private Vector2 networkCrosshairPos; 
    private List<Vector2> targetPositions = new List<Vector2>();
    private int targetsLeft = 8;
    
    // --- STATE ---
    public bool IsWifiBroken { get; private set; } = false;
    private bool amIFixer = false; 

    void Awake() { Instance = this; }

    void Update()
    {
        if (!IsWifiBroken) return;

        // --- NGƯỜI SỬA (ROUTER) ---
        if (amIFixer && panelRouter.activeSelf)
        {
            HandleMovement();
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                photonView.RPC("TryCaptureRPC", RpcTarget.MasterClient, crosshair.anchoredPosition);
            }
        }

        // --- NGƯỜI XEM (PC) ---
        if (!amIFixer && panelPC.activeSelf)
        {
            if(ghostCrosshair)
            {
                // Lerp nhanh hơn chút để đỡ bị lag hình (Time.deltaTime * 20f)
                ghostCrosshair.anchoredPosition = Vector2.Lerp(ghostCrosshair.anchoredPosition, networkCrosshairPos, Time.deltaTime * 20f);
            }
        }
    }

    // --- 1. SỰ KIỆN HỎNG WIFI ---
    public void TriggerWifiBreak()
    {
        photonView.RPC("SetWifiStateRPC", RpcTarget.AllBuffered, true);
    }

    [PunRPC]
    void SetWifiStateRPC(bool isBroken)
    {
        IsWifiBroken = isBroken;
        
        if (isBroken)
        {
            // Tìm và tắt máy tính nếu đang code
            InteractableObject[] allInteractables = FindObjectsOfType<InteractableObject>();
            bool wasCoding = false;
            
            foreach(var obj in allInteractables)
            {
                if (obj.type == InteractionType.Code && obj.IsAnyCodeMinigameActive())
                {
                    wasCoding = true;
                    obj.CloseAllMinigames(); 
                }
            }

            if (wasCoding) OpenPCPanel();
            
            // Master sinh mục tiêu
            if (PhotonNetwork.IsMasterClient) GenerateNewTargets();
        }
        else
        {
            CloseAllPanels();
            if (AudioManager.Instance) AudioManager.Instance.PlayWin();
        }
    }

    // --- 2. MỞ PANEL CHO NGƯỜI SỬA ---
    public void OpenRouterPanel()
    {
        if (!IsWifiBroken) return; 

        // --- CƯỚP QUYỀN ĐIỀU KHIỂN (QUAN TRỌNG) ---
        // Để Client có thể gửi vị trí tâm ngắm cho Master xem
        photonView.RequestOwnership();

        panelRouter.SetActive(true);
        amIFixer = true;
        
        if (PlayerController.LocalPlayerInstance) PlayerController.LocalPlayerInstance.canMove = false;
        if(crosshair) crosshair.anchoredPosition = Vector2.zero;
    }

    public void OpenPCPanel()
    {
        panelPC.SetActive(true);
        amIFixer = false; 
        if(pcStatusText) pcStatusText.text = "MẤT TÍN HIỆU! CẦN NGƯỜI SỬA ROUTER...";
        
        if (PlayerController.LocalPlayerInstance) 
            PlayerController.LocalPlayerInstance.canMove = false;
            
        // Render lại nếu đã có dữ liệu
        if (targetPositions.Count > 0) RenderTargets();
    }

    // --- 3. DI CHUYỂN ---
    void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal"); 
        float v = Input.GetAxis("Vertical");   

        Vector2 pos = crosshair.anchoredPosition;
        pos.x += h * moveSpeed * Time.deltaTime;
        pos.y += v * moveSpeed * Time.deltaTime;

        // Giới hạn trong khung
        float halfW = playAreaSize.x / 2f;
        float halfH = playAreaSize.y / 2f;
        pos.x = Mathf.Clamp(pos.x, -halfW, halfW);
        pos.y = Mathf.Clamp(pos.y, -halfH, halfH);

        crosshair.anchoredPosition = pos;
    }

    // --- 4. ĐỒNG BỘ VỊ TRÍ ---
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Nếu tôi đang mở bảng Router, tôi gửi vị trí của tôi đi
            if (panelRouter.activeSelf && crosshair != null)
            {
                stream.SendNext(crosshair.anchoredPosition);
            }
            else
            {
                stream.SendNext(Vector2.zero);
            }
        }
        else
        {
            // Người khác nhận vị trí về để vẽ Ghost Crosshair
            networkCrosshairPos = (Vector2)stream.ReceiveNext();
        }
    }

    // --- 5. LOGIC MỤC TIÊU ---
    void GenerateNewTargets()
    {
        targetPositions.Clear();
        float margin = 50f;
        float halfW = (playAreaSize.x / 2f) - margin;
        float halfH = (playAreaSize.y / 2f) - margin;

        for (int i = 0; i < 8; i++)
        {
            float x = Random.Range(-halfW, halfW);
            float y = Random.Range(-halfH, halfH);
            targetPositions.Add(new Vector2(x, y));
        }
        
        // Gửi danh sách cho mọi người
        photonView.RPC("SyncTargetsRPC", RpcTarget.AllBuffered, targetPositions.ToArray());
    }

    [PunRPC]
    void SyncTargetsRPC(Vector2[] positions)
    {
        targetPositions = new List<Vector2>(positions);
        targetsLeft = targetPositions.Count;
        
        // Chỉ render nếu đang ở màn hình PC
        if (!amIFixer && panelPC.activeSelf)
        {
            RenderTargets();
            if(pcStatusText) pcStatusText.text = $"MỤC TIÊU: {targetsLeft}";
        }
    }

    void RenderTargets()
    {
        if(targetContainer == null || targetPrefab == null) return;

        foreach (Transform child in targetContainer) Destroy(child.gameObject);
        foreach (Vector2 pos in targetPositions)
        {
            GameObject go = Instantiate(targetPrefab, targetContainer);
            go.GetComponent<RectTransform>().anchoredPosition = pos;
        }
    }

    [PunRPC]
    void TryCaptureRPC(Vector2 clickPos)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int hitIndex = -1;
        for (int i = 0; i < targetPositions.Count; i++)
        {
            if (Vector2.Distance(clickPos, targetPositions[i]) <= hitRadius)
            {
                hitIndex = i;
                break;
            }
        }

        if (hitIndex != -1)
        {
            targetPositions.RemoveAt(hitIndex);
            photonView.RPC("SyncTargetsRPC", RpcTarget.AllBuffered, targetPositions.ToArray());
            photonView.RPC("PlaySoundRPC", RpcTarget.All, true);
            
            if (targetPositions.Count == 0) 
                photonView.RPC("SetWifiStateRPC", RpcTarget.AllBuffered, false); 
        }
        else
        {
            photonView.RPC("PlaySoundRPC", RpcTarget.All, false);
            GenerateNewTargets(); 
        }
    }

    [PunRPC]
    void PlaySoundRPC(bool success)
    {
        if (AudioManager.Instance)
        {
            if (success) AudioManager.Instance.PlayClick(); 
            else AudioManager.Instance.PlayFail();   
        }
    }

    public void CloseAllPanels()
    {
        panelRouter.SetActive(false);
        panelPC.SetActive(false);
        
        if (PlayerController.LocalPlayerInstance)
            PlayerController.LocalPlayerInstance.canMove = true;
            
        amIFixer = false;
    }
}