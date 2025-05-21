using UnityEngine;

[CreateAssetMenu(menuName = "Abyssmal/Ability Data")]
public class AbilityData : ScriptableObject
{
    public string abilityName;
    public Sprite icon;
    public float cooldown;
}