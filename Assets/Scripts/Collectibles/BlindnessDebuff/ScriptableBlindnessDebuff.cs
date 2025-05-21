using UnityEngine;

[CreateAssetMenu(menuName = "Buffs/Blindness Debuff")]
public class ScriptableBlindnessDebuff : ScriptableBuff
{
    public override TimedBuff InitializeBuff(GameObject obj)
    {
        return new BlindnessTimedBuff(this, obj);
    }
}