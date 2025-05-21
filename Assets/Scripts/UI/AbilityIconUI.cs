using UnityEngine;
using UnityEngine.UI;

public class AbilityIconUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Image cooldownOverlay;

    public void Setup(AbilityData ability)
    {
        if (iconImage != null)
            iconImage.sprite = ability.icon;
        if (cooldownOverlay != null)
            cooldownOverlay.fillAmount = 0f;
    }

    public void SetCooldown(float cooldownPercent)
    {
        if (cooldownOverlay != null)
            cooldownOverlay.fillAmount = Mathf.Clamp01(cooldownPercent);
    }
}