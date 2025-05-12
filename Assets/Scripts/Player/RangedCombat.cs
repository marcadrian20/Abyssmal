using UnityEngine;

public class RangedCombat : PlayerCombat
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;

    public override void Attack()
    {
        // Fire a projectile
        if (projectilePrefab && firePoint)
            Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        Debug.Log("Ranged Attack!");
    }

    public override void SpecialAttack()
    {
        // Example: Multi-shot or charged shot
        Debug.Log("Ranged Special Attack!");
    }

    public override void UseAbility(int abilityIndex)
    {
        // Implement abilities (e.g., teleport, shield, etc.)
        Debug.Log($"Ranged Ability {abilityIndex} used!");
    }
}