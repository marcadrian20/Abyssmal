using UnityEngine;

public class PoisonTimedBuff : TimedBuff
{
    private PlayerHealth health;
    private float tickTimer;
    private ScriptablePoisonDebuff poisonBuff;

    public PoisonTimedBuff(ScriptableBuff buff, GameObject obj) : base(buff, obj)
    {
        Duration = buff.Duration;
        health = obj.GetComponent<PlayerHealth>();
        poisonBuff = (ScriptablePoisonDebuff)buff;
        tickTimer = 0f;
    }

    protected override void ApplyEffect()
    {
        // No immediate effect, handled in Tick
    }

    public override void End() { }

    public new void Tick(float delta)
    {
        base.Tick(delta);
        if (IsFinished) return;

        tickTimer -= delta;
        if (tickTimer <= 0f)
        {
            if (health != null)
                health.TakeDamage(poisonBuff.damagePerTick);
            tickTimer = poisonBuff.tickInterval;
        }
    }
}