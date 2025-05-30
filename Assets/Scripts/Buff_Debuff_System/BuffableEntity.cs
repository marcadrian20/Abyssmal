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

        bool buffsChanged = false;
        foreach (var buff in _buffs.Values.ToList())
        {
            buff.Tick(Time.deltaTime);
            if (buffBarUI != null)
            {
                float percent = Mathf.Clamp01(buff.GetDuration() / buff.Buff.Duration);
                buffBarUI.UpdateBuff(buff.Buff, percent);
            }
            if (buff.IsFinished)
            {
                _buffs.Remove(buff.Buff);
                buffsChanged = true;

            }
        }
        if (buffBarUI != null && buffsChanged)
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