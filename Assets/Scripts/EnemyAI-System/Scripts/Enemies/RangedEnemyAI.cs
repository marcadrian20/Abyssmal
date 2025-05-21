using UnityEngine;

public class RangedEnemyAI : EnemyAI
{
    [Header("Ranged Attack")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float attackRange = 5f;
    public float attackCooldown = 1f;
    private float lastAttackTime = 0f;

    protected void Update()
    {
        // base.Update();
        if (target == null) return;

        float distanceToTarget = Vector2.Distance(transform.position, target.position);
        if (distanceToTarget <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            Attack();
        }
    }

    public void Attack()
    {
        if (projectilePrefab && firePoint)
        {
            Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            lastAttackTime = Time.time;
        }
    }

    protected override void Move(Vector2 force)
    {
        // Implement movement logic if needed for ranged enemies
        base.Move(force);
    }
}