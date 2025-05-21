using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EnemyAnimation : MonoBehaviour
{
    private Animator animator;
    private Rigidbody2D rb;
    private MeleeEnemyAI meleeAI;

    // Animator parameter hashes for performance
    private readonly int AnimIsGrounded = Animator.StringToHash("IsGrounded");
    private readonly int AnimIsMoving = Animator.StringToHash("isMoving");
    private readonly int AnimVerticalVelocity = Animator.StringToHash("VerticalVelocity");
    private readonly int AnimAttack = Animator.StringToHash("Attack");
    private readonly int AnimJump = Animator.StringToHash("Jump");
    private readonly int AnimHurt = Animator.StringToHash("Hurt");
    private readonly int AnimDie = Animator.StringToHash("Die");

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        meleeAI = GetComponent<MeleeEnemyAI>();
    }
    void Update()
    {
        animator.SetBool(AnimIsMoving, Mathf.Abs(rb.linearVelocity.x) > 0.1f);
        animator.SetBool(AnimIsGrounded, meleeAI != null ? meleeAI.IsGrounded() : true);
        animator.SetFloat(AnimVerticalVelocity, rb.linearVelocity.y);
    }

    public void PlayAttack()
    {
        animator.SetTrigger(AnimAttack);
    }

    public void PlayJump()
    {
        animator.SetTrigger(AnimJump);
    }

    public void PlayHurt()
    {
        animator.SetTrigger(AnimHurt);
    }

    public void PlayDie()
    {
        animator.SetTrigger(AnimDie);
    }
}