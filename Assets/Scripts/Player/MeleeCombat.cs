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
    [Header("Special Attack")]
    [SerializeField] private float specialAttackDamage = 5f;
    [SerializeField] private float specialAttackRangeMultiplier = 2f;
    [SerializeField] private float specialAttackCooldown = 5f;
    // [SerializeField] private float ultimateTime = 0f;
    // [SerializeField] private float next_ultimateTime = 100f; // Amount needed to fill bar
    public float UltimateTime => ultimateTime;
    public float NextUltimateTime => next_ultimateTime;
    private bool canSpecialAttack => ultimateTime >= next_ultimateTime;


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
    public void AddUltimateCharge(float amount)
    {
        ultimateTime = Mathf.Clamp(ultimateTime + amount, 0, next_ultimateTime);
        if (FindFirstObjectByType<SpecialAttackBar>() != null)
            FindFirstObjectByType<SpecialAttackBar>().UpdateSpecialAttackBar();
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
        AddUltimateCharge(5f); // Add charge for each hit
        AudioManager.Instance.PlaySFX(AudioManager.Instance.attackClip); // Play sound effect
    }
    public override void SpecialAttack()
    {
        if (!canSpecialAttack) return;

        if (animator != null)
            animator.SetTrigger("SpecialAttack"); // Your special attack animation trigger

        // Do NOT deal damage here! Wait for the animation event to call DealSpecialDamage()
        ultimateTime = 0f; // Reset bar after use

        if (FindFirstObjectByType<SpecialAttackBar>() != null)
            FindFirstObjectByType<SpecialAttackBar>().UpdateSpecialAttackBar();
    }
    public void DealSpecialDamage()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            attackPoint.position,
            attackRange * specialAttackRangeMultiplier,
            enemyLayer
        );
        foreach (var hit in hits)
        {
            hit.SendMessage("TakeDamage", specialAttackDamage, SendMessageOptions.DontRequireReceiver);
        }
        AudioManager.Instance.PlaySFX(AudioManager.Instance.attackClip); // Or a special SFX
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