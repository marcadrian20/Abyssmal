using UnityEngine;

public abstract class EnemyCombat : MonoBehaviour
{
    // Called by AI or animation event
    public abstract void Attack();

    // For special attacks (optional)
    public virtual void SpecialAttack() { }

    // For future extensibility (e.g., abilities, projectiles)
    public virtual void UseAbility(int abilityIndex) { }
}