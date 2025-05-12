using UnityEngine;

public abstract class PlayerCombat : MonoBehaviour
{
    public abstract void Attack();
    public abstract void SpecialAttack();
    public abstract void UseAbility(int abilityIndex);
}