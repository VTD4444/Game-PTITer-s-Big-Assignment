using UnityEngine;
using Photon.Pun;

public class GameManager_Main : MonoBehaviour
{
    // Tạo biến Instance để PlayerController có thể gọi được
    public static GameManager_Main Instance;

    [Header("Config")]
    public Transform[] spawnPoints; 
    
    [Header("Character Database")]
    // Nếu bạn chưa làm Animator, hãy dùng Sprite[] cho đơn giản
    public RuntimeAnimatorController[] allCharacterAnimators; 
    public Sprite[] allCharacterSprites; 

    void Awake()
    {
        // Gán chính mình vào biến Instance
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (PhotonNetwork.IsConnected)
        {
            SpawnPlayer();
        }
    }

    void SpawnPlayer()
    {
        // Nếu chưa setup spawnPoints, dòng này sẽ lỗi. Hãy nhớ kéo thả SpawnPoint trong Editor sau.
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        int spawnIndex = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawnPoints.Length;
        Vector3 spawnPos = spawnPoints[spawnIndex].position;

        PhotonNetwork.Instantiate("Player", spawnPos, Quaternion.identity);
    }

    // Hàm mà PlayerController đang cố gọi đây
    public void ChangeSkin(PlayerController player, int index)
    {
        if (allCharacterAnimators != null && index >= 0 && index < allCharacterAnimators.Length)
        {
            if (allCharacterAnimators[index] != null)
            {
                // Dòng này sẽ thay thế toàn bộ bộ não animation của nhân vật
                player.anim.runtimeAnimatorController = allCharacterAnimators[index];
            }
        }
        // Code đổi Sprite tĩnh
        if (allCharacterSprites != null && index >= 0 && index < allCharacterSprites.Length)
        {
            if (allCharacterSprites[index] != null)
                player.spriteRenderer.sprite = allCharacterSprites[index];
        }
    }
}