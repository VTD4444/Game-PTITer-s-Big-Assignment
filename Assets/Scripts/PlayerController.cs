using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPun
{
    public static PlayerController LocalPlayerInstance;
    
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public bool canMove = true;
    
    [Header("Components")]
    public Rigidbody2D rb;
    public Animator anim;
    public SpriteRenderer spriteRenderer; 

    private Vector2 movement;
    
    void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (anim == null) anim = GetComponent<Animator>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        if (photonView.IsMine)
        {
            // --- Đăng ký bản thân là Người chơi cục bộ ---
            LocalPlayerInstance = this;
            Camera.main.transform.parent = this.transform;
            Camera.main.transform.localPosition = new Vector3(0, 0, -10);
        }

        object avatarCode;
        if (photonView.Owner.CustomProperties.TryGetValue("PlayerSelection", out avatarCode))
        {
            int index = (int)avatarCode;
            GameManager_Main.Instance.ChangeSkin(this, index);
        }
    }

    void Update()
    {
        if (photonView.IsMine)
        {
            // --- SỬA ĐỔI: Chỉ xử lý input nếu được phép (canMove == true) ---
            if (canMove)
            {
                ProcessInputs();
            }
            else
            {
                // Nếu bị khóa, đảm bảo nhân vật đứng im hoàn toàn
                movement = Vector2.zero;
                if (anim != null) anim.SetBool("IsMoving", false);
            }
        }
    }

    void FixedUpdate()
    {
        if (photonView.IsMine)
        {
            Move();
        }
    }

    void ProcessInputs()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        movement = new Vector2(moveX, moveY).normalized;

        if (anim != null)
        {
            // Cập nhật tham số theo yêu cầu của bạn
            // Chỉ cập nhật hướng nhìn khi nhân vật thực sự di chuyển
            // (Để khi đứng yên nó không quay mặt về mặc định 0,0)
            if (movement.magnitude > 0)
            {
                anim.SetFloat("InputX", movement.x);
                anim.SetFloat("InputY", movement.y);
            }

            anim.SetBool("IsMoving", movement.magnitude > 0);
        }
    }

    void Move()
    {
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
}