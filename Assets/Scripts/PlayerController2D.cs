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

    [Header("Glide")]
    public float glideGravityScale = 0.3f;
    public float maxFallSpeed = -2f;

    [Header("Dash")]
    public float dashSpeed = 12f;
    public float dashDuration = 0.15f;

    [Header("Wall Slide")]
    public float wallSlideSpeed = -1.5f;
    public Vector2 wallCheckOffset = new Vector2(0.6f, 0f);
    public float wallCheckRadius = 0.15f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayerMask;

    Rigidbody2D rb;
    PlayerInput playerInput;

    InputAction moveAction;
    InputAction jumpAction;

    Vector2 moveInput;
    bool isGrounded;
    bool isGliding;
    bool isDashing;
    bool isWallSliding;
    bool jumpRequested;

    int jumpsRemaining;
    float dashTimer;
    float normalGravityScale;
    Vector3 originalScale;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();

        normalGravityScale = rb.gravityScale;
        originalScale = transform.localScale;
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

        if (jumpAction.WasPressedThisFrame() && jumpsRemaining > 0 && !isDashing)
            jumpRequested = true;

        // Glide ONLY when not touching a wall
        isGliding =
            !isGrounded &&
            !IsTouchingWall() &&
            rb.linearVelocity.y <= 0f &&
            Keyboard.current != null &&
            Keyboard.current.wKey.isPressed;

        if (!isDashing &&
            Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            StartDash();
        }
    }

    void FixedUpdate()
    {
        CheckGrounded();

        if (isDashing)
        {
            DashMove();
            return;
        }

        // WALL SLIDE — input independent
        if (!isGrounded && IsTouchingWall() && rb.linearVelocity.y <= 0f)
        {
            ApplyWallSlide();
        }
        else
        {
            isWallSliding = false;
            Move();
        }

        if (jumpRequested)
        {
            Jump();
            jumpRequested = false;
        }

        if (isGliding)
            ApplyGlide();
        else
            rb.gravityScale = normalGravityScale;
    }

    // ================= MOVEMENT =================

    void Move()
    {
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
    }

    void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        jumpsRemaining--;
    }

    // ================= GLIDE =================

    void ApplyGlide()
    {
        rb.gravityScale = glideGravityScale;

        if (rb.linearVelocity.y < maxFallSpeed)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFallSpeed);
    }

    // ================= DASH =================

    void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        transform.localScale = new Vector3(originalScale.x, originalScale.y * 0.6f, originalScale.z);
    }

    void DashMove()
    {
        dashTimer -= Time.fixedDeltaTime;
        rb.linearVelocity = new Vector2(Mathf.Sign(moveInput.x == 0 ? 1 : moveInput.x) * dashSpeed, rb.linearVelocity.y);

        if (dashTimer <= 0f)
        {
            isDashing = false;
            transform.localScale = originalScale;
        }
    }

    // ================= WALL SLIDE =================

    void ApplyWallSlide()
    {
        isWallSliding = true;

        // THIS is the key: only clamp Y
        if (rb.linearVelocity.y < wallSlideSpeed)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, wallSlideSpeed);
    }

    bool IsTouchingWall()
    {
        Vector3 right = transform.position + (Vector3)wallCheckOffset;
        Vector3 left  = transform.position - (Vector3)wallCheckOffset;

        return Physics2D.OverlapCircle(right, wallCheckRadius, groundLayerMask) ||
               Physics2D.OverlapCircle(left, wallCheckRadius, groundLayerMask);
    }

    // ================= GROUND =================

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
}
