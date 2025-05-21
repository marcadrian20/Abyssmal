using UnityEngine;

public class RangedEnemyCombat : EnemyCombat
{
    [Header("Ranged Attack")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float attackCooldown = 1.5f;

    private float lastAttackTime = 0f;
    private EnemyAnimation enemyAnimation;

    void Awake()
    {
        enemyAnimation = GetComponent<EnemyAnimation>();
    }

    public override void Attack()
    {
        if (Time.time - lastAttackTime < attackCooldown)
            return;

        lastAttackTime = Time.time;

        if (enemyAnimation != null)
            enemyAnimation.PlayAttack();

        if (projectilePrefab && firePoint)
            Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
    }

    // Optional: visualize attack range in editor
    private void OnDrawGizmosSelected()
    {
        if (firePoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(firePoint.position, 0.5f); // Adjust size as needed
        }
    }
}