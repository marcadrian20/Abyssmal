using UnityEngine;

[CreateAssetMenu(menuName = "Buffs/Speed Buff")]
public class ScriptableSpeedBuff : ScriptableBuff
{
    public float speedMultiplier = 1.5f;

    public override TimedBuff InitializeBuff(GameObject obj)
    {
        return new SpeedTimedBuff(this, obj);
    }
}