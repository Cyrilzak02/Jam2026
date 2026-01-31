using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Jump")]
    public float jumpForce = 10f;
    public int maxJumps = 2;

    [Header("Glide / Parachute")]
    [Tooltip("Lower = floatier glide")]
    public float glideGravityScale = 0.3f;
    [Tooltip("Max downward speed while gliding (negative)")]
    public float maxFallSpeed = -2f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayerMask;

    // Components
    private Rigidbody2D rb;
    private PlayerInput playerInput;

    // Input actions
    private InputAction moveAction;
    private InputAction jumpAction;


    // State
    private Vector2 moveInput;
    private bool isGrounded;
    private bool jumpRequested;
    private bool isGliding;

    private int jumpsRemaining;
    private float normalGravityScale;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
        

        normalGravityScale = rb.gravityScale;
        jumpsRemaining = maxJumps;

        SetupGroundCheck();
    }

    void OnEnable()
    {
        
        moveAction = playerInput.actions.FindAction("Move", true);
        jumpAction = playerInput.actions.FindAction("Jump", true);
    }

    void Update()
{
    moveInput = moveAction.ReadValue<Vector2>();

    if (jumpAction.WasPressedThisFrame() && jumpsRemaining > 0)
    {
        jumpRequested = true;
    }

    
    isGliding = !isGrounded
                && rb.linearVelocity.y <= 0f
                && Keyboard.current != null
                && Keyboard.current.wKey.isPressed;
}


    void FixedUpdate()
    {
        CheckGrounded();
        Move();

        if (jumpRequested)
        {
            Jump();
            jumpRequested = false;
        }

        if (isGliding)
        {
            ApplyGlide();
        }
        else
        {
            rb.gravityScale = normalGravityScale;
        }
    }

    

    void Move()
    {
        Vector2 v = rb.linearVelocity;
        v.x = moveInput.x * moveSpeed;
        rb.linearVelocity = v;
    }

    void Jump()
    {
        
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        jumpsRemaining--;
    }

    

   void ApplyGlide()
{
    rb.gravityScale = glideGravityScale;

    if (rb.linearVelocity.y < maxFallSpeed)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFallSpeed);
    }
}

    

    void CheckGrounded()
    {
        bool wasGrounded = isGrounded;

        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayerMask
        );

       
        if (isGrounded && !wasGrounded)
        {
            jumpsRemaining = maxJumps;
            isGliding = false;
            rb.gravityScale = normalGravityScale;
        }
    }

    void SetupGroundCheck()
    {
        if (groundCheck == null)
        {
            GameObject gc = new GameObject("GroundCheck");
            gc.transform.SetParent(transform);
            gc.transform.localPosition = new Vector3(0, -0.8f, 0);
            groundCheck = gc.transform;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
