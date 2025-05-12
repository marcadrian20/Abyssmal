using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;


    // Animation parameter hashes for better performance
    private readonly int AnimIsGrounded = Animator.StringToHash("IsGrounded");
    private readonly int AnimIsMoving = Animator.StringToHash("IsMoving");
    private readonly int AnimVerticalVelocity = Animator.StringToHash("VerticalVelocity");
    private readonly int AnimJump = Animator.StringToHash("Jump");
    private readonly int AnimDash = Animator.StringToHash("Dash");
    private readonly int AnimRoll = Animator.StringToHash("Roll");
    private readonly int AnimHit = Animator.StringToHash("Hit");
    private Rigidbody2D rb;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }
    public void UpdateAnimations(bool isGrounded, Vector2 moveInput)
    {
        if (animator == null) return;

        // Update animator parameters
        animator.SetBool(AnimIsGrounded, isGrounded);
        animator.SetBool(AnimIsMoving, Mathf.Abs(moveInput.x) > 0.1f);
        animator.SetFloat(AnimVerticalVelocity, rb.linearVelocity.y);

        // // Flip sprite based on movement direction
        // if (moveInput.x > 0.1f && !spriteRenderer.flipX)
        //     spriteRenderer.flipX = true;
        // else if (moveInput.x < -0.1f && spriteRenderer.flipX)
        //     spriteRenderer.flipX = false;
    
    }

    public void PlayJumpAnimation()
    {
        if (animator == null) return;
        animator.SetTrigger(AnimJump);
    }
    
    public void PlayDashAnimation()
    {
        if (animator == null) return;
        animator.SetTrigger(AnimDash);
    }
    
    public void PlayRollAnimation()
    {
        if (animator == null) return;
        animator.SetTrigger(AnimRoll);
    }
    
    public void PlayHitAnimation()
    {
        if (animator == null) return;
        animator.SetTrigger(AnimHit);
    }
}
