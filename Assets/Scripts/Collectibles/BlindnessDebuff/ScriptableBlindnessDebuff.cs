using UnityEngine;

[CreateAssetMenu(menuName = "Debuffs/Blindness Debuff")]
public class ScriptableBlindnessDebuff : ScriptableBuff
{
    public override TimedBuff InitializeBuff(GameObject obj)
    {
        return new BlindnessTimedBuff(this, obj);
    }
}