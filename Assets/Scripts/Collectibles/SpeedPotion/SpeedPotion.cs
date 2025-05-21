using UnityEngine;

public class SpeedPotion : Collectible
{
    public ScriptableSpeedBuff speedBuff;

    public override void OnCollect(GameObject collector)
    {
        var buffable = collector.GetComponent<BuffableEntity>();
        if (buffable != null && speedBuff != null)
        {
            var timedBuff = speedBuff.InitializeBuff(collector);
            buffable.AddBuff(timedBuff);
        }
    }
}