using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class PlayerMovement : MonoBehaviour
{

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float acceleration = 50f;
    [SerializeField] private float deceleration = 60f;
    [SerializeField] private float velocityPower = 0.9f;
    [SerializeField] private float frictionAmount = 0.2f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 14f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;
    [SerializeField] private float jumpBufferTime = 0.2f;
    [SerializeField] private float coyoteTime = 0.2f;
    [SerializeField] private int maxJumps = 2;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 0.6f;
    [SerializeField] private GameObject dashTrailPrefab;

    [Header("Roll")]
    [SerializeField] private float rollSpeed = 12f;
    [SerializeField] private float rollDuration = 0.4f;
    [SerializeField] private float rollCooldown = 0.7f;
    [SerializeField] private LayerMask rollThroughLayers;

    [Header("Ground Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;



    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool isJumpPressed;
    private bool isJumpReleased = true;
    private bool isDashPressed;
    private bool isRollPressed;
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
    private Vector2 dashDirection;




    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
         if (IsGrounded())
        {
            lastGroundedTime = coyoteTime;
            jumpCount = 0;
        }
        else
        {
            lastGroundedTime -= Time.deltaTime;
        }

        // Jump buffer timing
        lastJumpPressedTime -= Time.deltaTime;

        // Check if should jump
        if (lastGroundedTime > 0 && lastJumpPressedTime > 0 && !isDashing && !isRolling)
        {
            Jump();
            lastJumpPressedTime = 0;
        }
    }

    void FixedUpdate()
    {
        if (isDashing || isRolling) return;

        // Apply movement
        ApplyMovement();
        // Apply jump physics
        ApplyJumpPhysics();
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        defaultGravityScale = rb.gravityScale;
        originalLayer = gameObject.layer;
        
    }


    public void UpdateMovement(Vector2 newMovementInput)
    {
        moveInput = newMovementInput;
    }
    public void Jump()
    {

        lastGroundedTime = 0;
        jumpCount++;
        
        // Reset Y velocity before applying jump force
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        // // Play jump effect and animation
        // if (jumpParticles != null)
        //     jumpParticles.Play();

        // animator?.SetTrigger(AnimJump);
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
        gameObject.layer = LayerMask.NameToLayer("PlayerRolling");

        // Apply roll velocity
        float rollDirection = isFacingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(rollDirection * rollSpeed, rb.linearVelocity.y);

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

    private bool IsGrounded()
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
    private void ApplyMovement()
    {
        // Calculate target speed
        float targetSpeed = moveInput.x * moveSpeed;

        // Calculate difference between current and target speed
        float speedDiff = targetSpeed - rb.linearVelocity.x;

        // Change acceleration rate depending on conditions
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;

        // Apply movement force
        float movement = Mathf.Pow(Mathf.Abs(speedDiff) * accelRate, velocityPower) * Mathf.Sign(speedDiff);
        rb.AddForce(movement * Vector2.right);

        // Apply friction when not trying to move
        if (Mathf.Abs(moveInput.x) < 0.01f && IsGrounded())
        {
            float frictionForce = Mathf.Min(Mathf.Abs(rb.linearVelocity.x), frictionAmount);
            frictionForce *= Mathf.Sign(rb.linearVelocity.x);
            rb.AddForce(Vector2.right * -frictionForce, ForceMode2D.Impulse);
        }

        // Handle direction facing
        if (moveInput.x > 0 && !isFacingRight)
            Flip();
        else if (moveInput.x < 0 && isFacingRight)
            Flip();
    }
    private void ApplyJumpPhysics()
    {
        // Apply increased gravity when falling
        if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = defaultGravityScale * fallMultiplier;
        }
        // Apply lower gravity when ascending but jump button released
        else if (rb.linearVelocity.y > 0 && !isJumpPressed)
        {
            rb.gravityScale = defaultGravityScale * lowJumpMultiplier;
        }
        else
        {
            rb.gravityScale = defaultGravityScale;
        }
    }

    // For debugging
     private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = IsGrounded() ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
    private void OnGUI()
    {
        // Uncomment for debugging
        /*
        GUILayout.BeginArea(new Rect(10, 10, 300, 100));
        GUILayout.Label($"Grounded: {IsGrounded()}, Jump Count: {jumpCount}");
        GUILayout.Label($"Velocity: {rb.velocity}");
        GUILayout.Label($"Can Dash: {canDash}, Can Roll: {canRoll}");
        GUILayout.EndArea();
        */
    }

}
