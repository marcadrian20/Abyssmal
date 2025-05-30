using UnityEngine;

public class MeleeEnemyCombat : EnemyCombat
{
    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 0.25f;
    [SerializeField] private int attackDamage = 20;
    [SerializeField] private float attackCooldown = 2.5f;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private LayerMask playerLayer;

    private float lastAttackTime = 0f;
    private EnemyAnimation enemyAnimation;
    private EnemyDebuffInflicter enemyDebuffInflicter;

    void Awake()
    {
        enemyAnimation = GetComponent<EnemyAnimation>();
        enemyDebuffInflicter = GetComponent<EnemyDebuffInflicter>();
    }

    public override void Attack()
    {
        if (Time.time - lastAttackTime < attackCooldown)
            return;

        lastAttackTime = Time.time;

        // Play attack animation
        if (enemyAnimation != null)
            enemyAnimation.PlayAttack();
    }
    public void DealDamage()
    {
        Collider2D hit = Physics2D.OverlapCircle(attackPoint.position, attackRange, playerLayer);
        if (hit != null)
        {
            Vector2 knockbackDir = (hit.transform.position - transform.position).normalized;
            var playerHealth = hit.GetComponent<PlayerHealth>();
            if (playerHealth != null)
                playerHealth.TakeDamage(attackDamage, knockbackDir);
            else
                hit.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
            enemyDebuffInflicter.InflictRandomDebuff(hit.gameObject);
        }
    }
    //visualize attack range in editor
    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}