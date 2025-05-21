using UnityEngine;
using System.Collections.Generic;

public class BuffBarUI : MonoBehaviour
{
    [SerializeField] private BuffIconUI iconPrefab;
    [SerializeField] private Transform iconParent;
    private Dictionary<ScriptableBuff, BuffIconUI> icons = new();

    public void SetBuffs(Dictionary<ScriptableBuff, TimedBuff> buffs)
    {
        // Remove old icons
        foreach (Transform child in iconParent) Destroy(child.gameObject);
        icons.Clear();

        // Add new icons
        foreach (var kvp in buffs)
        {
            var icon = Instantiate(iconPrefab, iconParent);
            icon.Setup(kvp.Key, kvp.Value.GetDuration() / kvp.Key.Duration);
            icons[kvp.Key] = icon;
        }
    }

    public void UpdateBuff(ScriptableBuff buff, float percent)
    {
        if (icons.TryGetValue(buff, out var icon))
            icon.UpdateDuration(percent);
    }
}