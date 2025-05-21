using UnityEngine;

public abstract class PlayerCombat : MonoBehaviour
{
    public float ultimateTime;  // Time until the ultimate ability is ready
    public float next_ultimateTime; // Time until the next ultimate ability can be used
    public abstract void Attack();
    public abstract void SpecialAttack();
    public abstract void UseAbility(int abilityIndex);
}