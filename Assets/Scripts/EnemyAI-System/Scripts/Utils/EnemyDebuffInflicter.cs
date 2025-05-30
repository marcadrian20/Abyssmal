using UnityEngine;
using System.Collections.Generic;

public class EnemyDebuffInflicter : MonoBehaviour
{
    public List<ScriptableBuff> possibleDebuffs;

    public void InflictRandomDebuff(GameObject player)
    {
        if (possibleDebuffs == null || possibleDebuffs.Count == 0) return;
        var debuff = possibleDebuffs[Random.Range(0, possibleDebuffs.Count)];
        Debug.Log($"Inflicting {debuff.name}");
        var buffable = player.GetComponent<BuffableEntity>();
        if (buffable != null)
            buffable.AddBuff(debuff.InitializeBuff(player));
        Debug.Log($"Inflicted {debuff.name} on {player.name}");
    }
}