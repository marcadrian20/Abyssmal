using UnityEngine;

[CreateAssetMenu(menuName = "Buffs/Berserk Buff")]
public class ScriptableBerserkBuff : ScriptableBuff
{
    public float damageMultiplier = 2f;
    public float healthMultiplier = 0.5f;

    public override TimedBuff InitializeBuff(GameObject obj)
    {
        return new BerserkTimedBuff(this, obj);
    }
}