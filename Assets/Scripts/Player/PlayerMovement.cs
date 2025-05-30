using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float airMoveSpeed = 6f;
    [SerializeField] private float acceleration = 15f;
    [SerializeField] private float airAcceleration = 8f;
    [SerializeField] private float deceleration = 25f;
    [SerializeField] private float airDeceleration = 15f;
    [SerializeField] private float frictionAmount = 0.9f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;
    [SerializeField] private float jumpBufferTime = 0.15f;
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private int maxJumps = 2;
    [SerializeField] private float jumpCutMultiplier = 0.5f; // NEW: For variable jump height

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 0.8f;
    [SerializeField] private bool dashResetsOnGround = true; // NEW: Reset dash on landing
    [SerializeField] private GameObject dashTrailPrefab;

    [Header("Roll")]
    [SerializeField] private float rollSpeed = 12f;
    [SerializeField] private float rollDuration = 0.3f;
    [SerializeField] private float rollCooldown = 0.5f;

    [Header("WallActions")]
    [SerializeField] private float wallSlideSpeed = 2f;
    [SerializeField] private float wallJumpForce = 12f;
    [SerializeField] private float wallJumpHorizontalForce = 8f;
    [SerializeField] private float wallJumpTime = 0.15f;
    [SerializeField] private float wallStickTime = 0.25f; // NEW: Brief stick time
    [SerializeField] private Transform wallCheck;
    [SerializeField] private LayerMask wallLayer;

    [Header("Ground Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.1f;
    [SerializeField] private LayerMask groundLayer;

    // Private variables
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool isJumpPressed;
    private bool isJumpHeld;
    private float lastGroundedTime;
    private float lastJumpPressedTime;
    private int jumpCount;
    private bool isDashing;
    private bool isRolling;
    private bool canDash = true;
    private bool canRoll = true;
    private bool isFacingRight = true;
    private float defaultGravityScale;
    private SpriteRenderer spriteRenderer;

    // Wall mechanics
    private bool isTouchingWall;
    private bool isWallSliding;
    private bool wallJumping;
    private float wallJumpCounter;
    private float wallStickCounter;
    private int wallDirection;

    // NEW: Input buffering for better responsiveness
    private float lastDashPressedTime;
    private float lastRollPressedTime;
    private const float actionBufferTime = 0.1f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        defaultGravityScale = rb.gravityScale;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        CheckGrounded();
        CheckWallInteraction();
        HandleJumpBuffer();
        HandleActionBuffers();
        UpdateTimers();
    }

    void FixedUpdate()
    {
        if (isDashing || isRolling) return;

        ApplyMovement();
        ApplyJumpPhysics();
        HandleWallSlide();
    }

    private void CheckGrounded()
    {
        bool wasGrounded = lastGroundedTime > 0;
        bool isGrounded = IsGrounded();

        if (isGrounded)
        {
            lastGroundedTime = coyoteTime;
            jumpCount = 0;

            // Reset dash on ground if enabled
            if (dashResetsOnGround && !wasGrounded)
                canDash = true;
        }
        else
        {
            lastGroundedTime -= Time.deltaTime;
        }
    }

    private void CheckWallInteraction()
    {
        isTouchingWall = Physics2D.OverlapCircle(wallCheck.position, 0.15f, wallLayer);
        wallDirection = isTouchingWall ? (isFacingRight ? 1 : -1) : 0;

        bool canWallSlide = isTouchingWall && !IsGrounded() && rb.linearVelocity.y < 0;

        if (canWallSlide && moveInput.x * wallDirection > 0 && !wallJumping)
        {
            if (!isWallSliding)
            {
                isWallSliding = true;
                wallStickCounter = wallStickTime;
            }
        }
        else
        {
            isWallSliding = false;
            wallStickCounter = 0;
        }
    }

    private void HandleJumpBuffer()
    {
        if (lastJumpPressedTime > 0)
            lastJumpPressedTime -= Time.deltaTime;

        // Improved jump logic with better buffering
        if (lastGroundedTime > 0 && lastJumpPressedTime > 0 && !isDashing && !isRolling)
        {
            Jump();
            lastJumpPressedTime = 0;
        }
        else if (isWallSliding && lastJumpPressedTime > 0)
        {
            WallJump();
            lastJumpPressedTime = 0;
        }
    }

    private void HandleActionBuffers()
    {
        // Dash buffer
        if (lastDashPressedTime > 0)
        {
            lastDashPressedTime -= Time.deltaTime;
            if (canDash && !isDashing && !isRolling)
            {
                Dash();
                lastDashPressedTime = 0;
            }
        }

        // Roll buffer
        if (lastRollPressedTime > 0)
        {
            lastRollPressedTime -= Time.deltaTime;
            if (canRoll && !isRolling && !isDashing && IsGrounded())
            {
                Roll();
                lastRollPressedTime = 0;
            }
        }
    }

    private void UpdateTimers()
    {
        if (wallJumping)
        {
            wallJumpCounter -= Time.deltaTime;
            if (wallJumpCounter <= 0)
                wallJumping = false;
        }

        if (wallStickCounter > 0)
            wallStickCounter -= Time.deltaTime;
    }

    private void HandleWallSlide()
    {
        if (isWallSliding)
        {
            float slideSpeed = wallStickCounter > 0 ? 0 : wallSlideSpeed;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Clamp(rb.linearVelocity.y, -slideSpeed, float.MaxValue));
        }
    }

    // Public input methods
    public void UpdateMovement(Vector2 newMovementInput)
    {
        moveInput = newMovementInput;
    }

    public void Jump()
    {
        if (isWallSliding)
        {
            WallJump();
            return;
        }

        if (lastGroundedTime > 0)
        {
            PerformJump();
            jumpCount = 1;
        }
        else if (jumpCount < maxJumps && !isDashing && !isRolling)
        {
            PerformJump();
            jumpCount++;
        }
    }

    public void BufferJump()
    {
        lastJumpPressedTime = jumpBufferTime;
    }

    public void SetJumpPressed(bool pressed)
    {
        isJumpPressed = pressed;
        isJumpHeld = pressed;

        if (pressed)
            BufferJump();
        else if (!pressed && rb.linearVelocity.y > 0)
        {
            // Cut jump short for variable height
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }
    }

    public void BufferDash()
    {
        lastDashPressedTime = actionBufferTime;
    }

    public void BufferRoll()
    {
        lastRollPressedTime = actionBufferTime;
    }

    private void PerformJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        lastGroundedTime = 0; // Prevent double jumps
    }

    public void WallJump()
    {
        wallJumping = true;
        wallJumpCounter = wallJumpTime;
        isWallSliding = false;

        Vector2 jumpDirection = new Vector2(-wallDirection * wallJumpHorizontalForce, wallJumpForce);
        rb.linearVelocity = jumpDirection;

        // Auto-flip away from wall
        if (wallDirection > 0 && isFacingRight || wallDirection < 0 && !isFacingRight)
            Flip();
    }

    public void Dash()
    {
        if (canDash && !isDashing && !isRolling)
        {
            var playerAnimation = GetComponent<PlayerAnimation>();
            playerAnimation?.PlayDashAnimation();
            StartCoroutine(DashRoutine());
        }
    }

    public void Roll()
    {
        if (canRoll && !isRolling && !isDashing && IsGrounded())
        {
            var playerAnimation = GetComponent<PlayerAnimation>();
            playerAnimation?.PlayRollAnimation();
            StartCoroutine(RollRoutine());
        }
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;
        canDash = false;

        // Improved dash physics
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0;

        Vector2 dashDir = moveInput.normalized;
        if (dashDir == Vector2.zero)
            dashDir = isFacingRight ? Vector2.right : Vector2.left;

        rb.linearVelocity = dashDir * dashSpeed;

        // Make player invulnerable during dash
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemies"), true);

        yield return new WaitForSeconds(dashDuration);

        isDashing = false;
        rb.gravityScale = originalGravity;
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemies"), false);

        // Only start cooldown if dash doesn't reset on ground
        if (!dashResetsOnGround)
        {
            yield return new WaitForSeconds(dashCooldown);
            canDash = true;
        }
    }

    private IEnumerator RollRoutine()
    {
        isRolling = true;
        canRoll = false;

        float rollDirection = isFacingRight ? 1f : -1f;
        Vector2 rollVelocity = new Vector2(rollDirection * rollSpeed, rb.linearVelocity.y);

        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemies"), true);

        float timer = 0;
        while (timer < rollDuration)
        {
            rb.linearVelocity = new Vector2(rollVelocity.x, rb.linearVelocity.y);
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        isRolling = false;
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemies"), false);

        yield return new WaitForSeconds(rollCooldown);
        canRoll = true;
    }

    private void ApplyMovement()
    {
        float targetSpeed = moveInput.x * (IsGrounded() ? moveSpeed : airMoveSpeed);
        float speedDiff = targetSpeed - rb.linearVelocity.x;

        // Choose acceleration/deceleration based on ground state
        float accelRate;
        if (IsGrounded())
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        else
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? airAcceleration : airDeceleration;

        // Improved movement calculation
        float movement = speedDiff * accelRate;

        // Don't apply movement during wall jump briefly
        if (!wallJumping || wallJumpCounter <= wallJumpTime * 0.5f)
            rb.AddForce(movement * Vector2.right);

        // Enhanced friction
        if (Mathf.Abs(moveInput.x) < 0.01f && IsGrounded())
        {
            float friction = rb.linearVelocity.x * frictionAmount;
            rb.AddForce(-friction * Vector2.right, ForceMode2D.Force);
        }

        // Improved flipping logic
        if (!wallJumping && Mathf.Abs(moveInput.x) > 0.1f)
        {
            if (moveInput.x > 0 && !isFacingRight)
                Flip();
            else if (moveInput.x < 0 && isFacingRight)
                Flip();
        }
    }

    private void ApplyJumpPhysics()
    {
        if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = defaultGravityScale * fallMultiplier;
        }
        else if (rb.linearVelocity.y > 0 && !isJumpHeld)
        {
            rb.gravityScale = defaultGravityScale * lowJumpMultiplier;
        }
        else
        {
            rb.gravityScale = defaultGravityScale;
        }
    }

    public bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;

        if (spriteRenderer != null)
            spriteRenderer.flipX = !isFacingRight;
        else
            transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
    }

    // Gizmos for debugging
    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = IsGrounded() ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        if (wallCheck != null)
        {
            Gizmos.color = isTouchingWall ? Color.blue : Color.yellow;
            Gizmos.DrawWireSphere(wallCheck.position, 0.15f);
        }
    }
}