using UnityEngine;

public class HealPotion : Collectible
{
    public int healAmount = 2;

    public override void OnCollect(GameObject collector)
    {
        var health = collector.GetComponent<PlayerHealth>();
        if (health != null)
            health.Heal(Random.Range(10, 35));//we calculate a random value to add to the current health
    }
}