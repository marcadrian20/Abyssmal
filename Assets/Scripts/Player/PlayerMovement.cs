using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class PlayerMovement : MonoBehaviour
{

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 50f;
    [SerializeField] private float airMoveSpeed = 50f; // NEW: Air control
    [SerializeField] private float acceleration = 25f;
    [SerializeField] private float deceleration = 20f;
    [SerializeField] private float velocityPower = 0.6f;
    [SerializeField] private float frictionAmount = 0.8f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float fallMultiplier = 1f;
    [SerializeField] private float lowJumpMultiplier = 2f;
    [SerializeField] private float jumpBufferTime = 0.2f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private int maxJumps = 1;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 100f;
    [SerializeField] private float dashDuration = 0.3f;
    [SerializeField] private float dashCooldown = 0.6f;
    [SerializeField] private GameObject dashTrailPrefab;

    [Header("Roll")]
    [SerializeField] private float rollSpeed = 60f;
    [SerializeField] private float rollDuration = 0.25f;
    [SerializeField] private float rollCooldown = 0.5f;

    [Header("WallActions")]
    [SerializeField] private float wallSlideSpeed = 20f;
    [SerializeField] private float wallJumpForce = 3f;
    [SerializeField] private float wallJumpHorizontalForce = 3f; // NEW: Stronger horizontal push
    [SerializeField] private Transform wallCheck;
    [SerializeField] private LayerMask wallLayer;
    private bool isTouchingWall;
    private bool isWallSliding;
    private bool wallJumping;
    private float wallJumpTime = 0.2f; // NEW: Prevents sticking after wall jump
    private float wallJumpCounter;

    [Header("Ground Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.05f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool isJumpPressed;
    private bool isJumpReleased = true;
    private float lastGroundedTime;
    private float lastJumpPressedTime;
    private int jumpCount;
    private bool isDashing;
    private bool isRolling;
    private bool canDash = true;
    private bool canRoll = true;
    private bool isFacingRight = true;
    private float defaultGravityScale;
    private int originalLayer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        defaultGravityScale = rb.gravityScale;
        originalLayer = gameObject.layer;
    }

    void Update()
    {
        // --- Ground & Jump Buffer ---
        if (IsGrounded())
        {
            lastGroundedTime = coyoteTime;
            jumpCount = 0;
        }
        else
        {
            lastGroundedTime -= Time.deltaTime;
        }

        if (lastJumpPressedTime > 0)
            lastJumpPressedTime -= Time.deltaTime;

        // --- Wall Slide ---
        isTouchingWall = Physics2D.OverlapCircle(wallCheck.position, 0.2f, wallLayer);
        isWallSliding = isTouchingWall && !IsGrounded() && moveInput.x != 0 && rb.linearVelocity.y < 0 && !wallJumping;
        if (isWallSliding)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Clamp(rb.linearVelocity.y, -wallSlideSpeed, float.MaxValue));

        // --- Wall Jump Timer ---
        if (wallJumping)
        {
            wallJumpCounter -= Time.deltaTime;
            if (wallJumpCounter <= 0)
                wallJumping = false;
        }

        // --- Jump Buffer & Coyote Time ---
        if (lastGroundedTime > 0 && lastJumpPressedTime > 0 && !isDashing && !isRolling)
        {
            Jump();
            lastJumpPressedTime = 0;
        }
    }

    void FixedUpdate()
    {
        if (isDashing || isRolling) return;
        ApplyMovement();
        ApplyJumpPhysics();
    }


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
    private void PerformJump()
    {
        // Reset Y velocity before applying jump force
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }
    public void WallJump()
    {
        wallJumping = true;
        wallJumpCounter = wallJumpTime;
        float dir = isFacingRight ? -1 : 1;
        rb.linearVelocity = new Vector2(dir * wallJumpHorizontalForce, wallJumpForce);
        // Flip facing direction
        if ((isFacingRight && moveInput.x < 0) || (!isFacingRight && moveInput.x > 0))
            Flip();
    }
    public void Dash()
    {
        if (canDash && !isDashing && !isRolling)
        {
            StartCoroutine(DashRoutine());

        }
    }
    public void Roll()
    {
        if (canRoll && !isRolling && !isDashing && IsGrounded())
        {
            StartCoroutine(RollRoutine());
        }
    }

    ///Coroutines/////
    private IEnumerator DashRoutine()
    {
        isDashing = true;
        canDash = false;
        int originalLayer = gameObject.layer;
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast"); // A layer that won't collide with damage sources

        // Save current velocity and gravity
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0;

        // Calculate dash direction
        Vector2 dashDir = moveInput.normalized;
        if (dashDir == Vector2.zero)
            dashDir = isFacingRight ? Vector2.right : Vector2.left;

        // Apply dash force
        rb.linearVelocity = dashDir * dashSpeed;

        // Play dash effect
        // if (dashParticles != null)
        //     dashParticles.Play();

        // animator?.SetTrigger(AnimDash);

        // // Trail effect
        // if (dashTrailPrefab != null)
        // {
        //     GameObject trail = Instantiate(dashTrailPrefab, transform.position, Quaternion.identity);
        //     Destroy(trail, dashDuration + 0.1f);
        // }

        yield return new WaitForSeconds(dashDuration);

        // End dash
        isDashing = false;
        rb.gravityScale = originalGravity;
        gameObject.layer = originalLayer;
        // Cooldown
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    private IEnumerator RollRoutine()
    {
        isRolling = true;
        canRoll = false;

        // Save original layer and change to pass through certain obstacles
        int originalLayerValue = gameObject.layer;
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast"); // A layer that won't collide with damage sources

        // Apply roll velocity
        // float rollDirection = isFacingRight ? 1f : -1f;
        // rb.linearVelocity = new Vector2(rollDirection * rollSpeed, rb.linearVelocity.y);

        // // Play roll effect
        // if (rollParticles != null)
        //     rollParticles.Play();

        // animator?.SetTrigger(AnimRoll);

        yield return new WaitForSeconds(rollDuration);

        // End roll
        isRolling = false;
        gameObject.layer = originalLayerValue;

        // Cooldown
        yield return new WaitForSeconds(rollCooldown);
        canRoll = true;
    }

    public bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }



    private void Flip()
    {
        isFacingRight = !isFacingRight;

        // Either flip the sprite or the entire transform
        // if (spriteRenderer != null)
        //     spriteRenderer.flipX = !isFacingRight;
        // else
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
    }


    //////////////Physics update methods////////////
    // --- Movement & Physics ---
    private void ApplyMovement()
    {
        float targetSpeed = moveInput.x * (IsGrounded() ? moveSpeed : airMoveSpeed);
        float speedDiff = targetSpeed - rb.linearVelocity.x;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        float movement = Mathf.Pow(Mathf.Abs(speedDiff) * accelRate, velocityPower) * Mathf.Sign(speedDiff);

        if (!wallJumping)
            rb.AddForce(movement * Vector2.right);

        // Friction and anti-slope-slide
        if (Mathf.Abs(moveInput.x) < 0.01f && IsGrounded())
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            float frictionForce = Mathf.Min(Mathf.Abs(rb.linearVelocity.x), frictionAmount);
            frictionForce *= Mathf.Sign(rb.linearVelocity.x);
            rb.AddForce(Vector2.right * -frictionForce, ForceMode2D.Impulse);
        }

        // Flip facing
        if (moveInput.x > 0 && !isFacingRight)
            Flip();
        else if (moveInput.x < 0 && isFacingRight)
            Flip();
    }

    private void ApplyJumpPhysics()
    {
        // Variable jump height
        if (rb.linearVelocity.y < 0)
            rb.gravityScale = defaultGravityScale * fallMultiplier;
        else if (rb.linearVelocity.y > 0 && !isJumpPressed)
            rb.gravityScale = defaultGravityScale * lowJumpMultiplier;
        else
            rb.gravityScale = defaultGravityScale;
    }
    public void BufferJump()
    {
        lastJumpPressedTime = jumpBufferTime;
    }
    public void SetJumpPressed(bool pressed)
    {
        isJumpPressed = pressed;
        if (pressed)
            BufferJump();
    }
    // For debugging
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
            Gizmos.DrawWireSphere(wallCheck.position, 0.2f);
        }
    }
    // private void OnGUI()
    // {
    //     // Uncomment for debugging

    //     GUILayout.BeginArea(new Rect(10, 10, 300, 100));
    //     GUILayout.Label($"Grounded: {IsGrounded()}, {lastGroundedTime} ,Jump Count: {jumpCount}");
    //     GUILayout.Label($"Velocity: {rb.linearVelocity}");
    //     GUILayout.Label($"Can Dash: {canDash}, Can Roll: {canRoll}");
    //     GUILayout.EndArea();

    // }

}
