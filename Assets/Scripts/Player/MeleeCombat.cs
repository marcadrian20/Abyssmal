using UnityEngine;

public class MeleeCombat : PlayerCombat
{
    [Header("Combo Settings")]
    [SerializeField] private int maxCombo = 2;
    [SerializeField] private float attackCooldown = 0.5f; // Minimum time between attacks
    private float nextAttackTime = 0f;
    [SerializeField] private float comboResetTime = 1f;
    [SerializeField] private float attackRange = 0.4f;
    [SerializeField] private float attackDamage = 1f;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private LayerMask enemyLayer;

    private int currentCombo = 0;
    private float lastAttackTime = 0f;
    private Animator animator;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    public override void Attack()
    {
        if (Time.time < nextAttackTime)
            return; // Still in cooldown, ignore input
        if (Time.time - lastAttackTime > comboResetTime)
            currentCombo = 1; // Start new combo at 1
        else
            currentCombo = Mathf.Clamp(currentCombo + 1, 1, maxCombo); // Increment, clamp to maxCombo

        lastAttackTime = Time.time;
        nextAttackTime = Time.time + attackCooldown; // Set next allowed attack time
        // Trigger attack animation based on combo stage
        if (animator != null)
            animator.SetTrigger("Attack" + currentCombo); // Triggers: Attack1, Attack2, Attack3
        else
            Debug.Log("No animator");



        Debug.Log($"Melee Attack! Combo stage: {currentCombo}");
    }
    public void DealDamage()
    {
        // Detect enemies in range and apply damage
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);
        foreach (var hit in hits)
        {
            EnemyHealth enemyHealth = hit.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                // Calculate knockback direction: from player to enemy
                Vector2 knockbackDir = (hit.transform.position - transform.position).normalized;
                enemyHealth.TakeDamage((int)attackDamage, knockbackDir);
            }
            else
            {
                // Fallback for other damage receivers
                hit.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
            }
        }
        AudioManager.Instance.PlaySFX(AudioManager.Instance.attackClip); // Play sound effect
    }
    public override void SpecialAttack()
    {
        // Example: Heavy slash or area attack
        if (animator != null)
            animator.SetTrigger("SpecialAttack");

        // Area damage
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRange * 1.5f, enemyLayer);
        foreach (var hit in hits)
        {
            hit.SendMessage("TakeDamage", attackDamage * 2f, SendMessageOptions.DontRequireReceiver);
        }

        Debug.Log("Melee Special Attack!");
    }

    public override void UseAbility(int abilityIndex)
    {
        // Implement abilities (e.g., dash, parry, etc.)
        Debug.Log($"Melee Ability {abilityIndex} used!");
        // Example: if (abilityIndex == 0) Parry(); else if (abilityIndex == 1) DashAttack();
    }

    // Optional: visualize attack range in editor
    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}