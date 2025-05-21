using UnityEngine;

public class BerserkTimedBuff : TimedBuff
{
    private MeleeCombat meleeCombat;
    private PlayerHealth health;
    private float originalDamage;
    private int originalMaxHealth;

    public BerserkTimedBuff(ScriptableBuff buff, GameObject obj) : base(buff, obj)
    {
        Duration = buff.Duration;
        meleeCombat = obj.GetComponent<MeleeCombat>();
        health = obj.GetComponent<PlayerHealth>();
    }

    protected override void ApplyEffect()
    {
        if (meleeCombat != null && EffectStacks == 0)
        {
            var berserkBuff = (ScriptableBerserkBuff)Buff;
            var attackDamageField = typeof(MeleeCombat).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            originalDamage = (float)attackDamageField.GetValue(meleeCombat);
            attackDamageField.SetValue(meleeCombat, originalDamage * berserkBuff.damageMultiplier);
        }
        if (health != null && EffectStacks == 0)
        {
            var maxHealthField = typeof(PlayerHealth).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            originalMaxHealth = (int)maxHealthField.GetValue(health);
            int newMaxHealth = Mathf.Max(1, Mathf.RoundToInt(originalMaxHealth * ((ScriptableBerserkBuff)Buff).healthMultiplier));
            maxHealthField.SetValue(health, newMaxHealth);
            // Clamp current health if needed
            if (health.GetCurrentHealth() > newMaxHealth)
                health.Heal(newMaxHealth - health.GetCurrentHealth());
        }
    }

    public override void End()
    {
        if (meleeCombat != null)
        {
            var attackDamageField = typeof(MeleeCombat).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            attackDamageField.SetValue(meleeCombat, originalDamage);
        }
        if (health != null)
        {
            var maxHealthField = typeof(PlayerHealth).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            maxHealthField.SetValue(health, originalMaxHealth);
            if (health.GetCurrentHealth() > originalMaxHealth)
                health.Heal(originalMaxHealth - health.GetCurrentHealth());
        }
    }
}