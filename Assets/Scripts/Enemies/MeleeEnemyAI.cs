using UnityEngine;

public class MeleeEnemyAI : EnemyAI
{
    [Header("Melee Attack")]
    public float attackRange = 1.2f;
    public float attackCooldown = 1f;
    public int attackDamage = 1;
    private float lastAttackTime = 0f;
    [SerializeField] private Transform attackPoint;
    [Header("Jumping")]
    public float jumpForce = 7f;
    private float lastJumpTime = 0f;
    public float jumpCooldown = 0.7f; // seconds

    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;
    private EnemyAnimation enemyAnimation;

    void Awake()
    {
        enemyAnimation = GetComponent<EnemyAnimation>();
    }
    void Update()
    {
        if (target == null) return;

        float distanceToTarget = Vector2.Distance(transform.position, target.position);
        if (distanceToTarget <= attackRange && Time.time - lastAttackTime > attackCooldown)
        {
            Attack();
            lastAttackTime = Time.time;
        }
    }

    protected override void TryJump(Vector2 nextWaypoint)
    {
        if (IsGrounded() && nextWaypoint.y > (rb.position.y + 0.4f) && Time.time - lastJumpTime > jumpCooldown)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            lastJumpTime = Time.time;
            if (enemyAnimation != null)
                enemyAnimation.PlayJump();
        }
    }

    public bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }
    public override void Attack()
    {
        // Simple attack: deal damage if in range
        var hit = Physics2D.OverlapCircle(attackPoint.position, attackRange, LayerMask.GetMask("Player"));
        if (hit != null)
        {
            hit.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
        }
        if (enemyAnimation != null)
            enemyAnimation.PlayAttack();
    }


    // Optional: visualize attack range in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}