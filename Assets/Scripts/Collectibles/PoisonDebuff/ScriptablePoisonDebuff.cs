using UnityEngine;

[CreateAssetMenu(menuName = "Debuffs/Poison Debuff")]
public class ScriptablePoisonDebuff : ScriptableBuff
{
    public int damagePerTick = 1;
    public float tickInterval = 1f;

    public override TimedBuff InitializeBuff(GameObject obj)
    {
        return new PoisonTimedBuff(this, obj);
    }
}