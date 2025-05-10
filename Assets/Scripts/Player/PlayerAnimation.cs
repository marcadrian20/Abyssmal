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
    }
    public void UpdateAnimations()
    {
        if (animator == null) return;

        // animator.SetBool(AnimIsGrounded, IsGrounded());
        // animator.SetBool(AnimIsMoving, Mathf.Abs(moveInput.x) > 0.1f);
        // animator.SetFloat(AnimVerticalVelocity, rb.velocity.y);
    }
}
