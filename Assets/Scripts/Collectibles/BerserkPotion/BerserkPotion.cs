using UnityEngine;

public class BerserkPotion : Collectible
{
    public ScriptableBerserkBuff berserkBuff;

    public override void OnCollect(GameObject collector)
    {
        var buffable = collector.GetComponent<BuffableEntity>();
        if (buffable != null && berserkBuff != null)
        {
            var timedBuff = berserkBuff.InitializeBuff(collector);
            buffable.AddBuff(timedBuff);
        }
    }
}