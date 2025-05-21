using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class AbilityBarUI : MonoBehaviour
{
    [SerializeField] private AbilityIconUI iconPrefab;
    [SerializeField] private Transform iconParent;
    private Dictionary<string, AbilityIconUI> icons = new();

    public void SetAbilities(List<AbilityData> abilities)
    {
        foreach (Transform child in iconParent) Destroy(child.gameObject);
        icons.Clear();

        foreach (var ability in abilities)
        {
            var icon = Instantiate(iconPrefab, iconParent);
            icon.Setup(ability);
            icons[ability.abilityName] = icon;
        }
    }

    public void UpdateCooldown(string abilityName, float cooldown)
    {
        if (icons.TryGetValue(abilityName, out var icon))
            icon.SetCooldown(cooldown);
    }
}