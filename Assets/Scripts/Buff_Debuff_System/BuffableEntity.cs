using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BuffableEntity : MonoBehaviour
{
    private readonly Dictionary<ScriptableBuff, TimedBuff> _buffs = new Dictionary<ScriptableBuff, TimedBuff>();
    [SerializeField] private BuffBarUI buffBarUI;
    void Update()
    {
        //OPTIONAL, return before updating each buff if game is paused
        //if (Game.isPaused)
        //    return;

        foreach (var buff in _buffs.Values.ToList())
        {
            buff.Tick(Time.deltaTime);
            if (buff.IsFinished)
            {
                _buffs.Remove(buff.Buff);
            }
        }
        if (buffBarUI != null)
            buffBarUI.SetBuffs(_buffs);
    }

    public void AddBuff(TimedBuff buff)
    {
        if (_buffs.ContainsKey(buff.Buff))
        {
            _buffs[buff.Buff].Activate();
        }
        else
        {
            _buffs.Add(buff.Buff, buff);
            buff.Activate();
        }
    }
}