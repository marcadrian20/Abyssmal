using UnityEngine;
using UnityEngine.UI;

public class BuffIconUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Image durationFill;

    public void Setup(ScriptableBuff buff, float duration)
    {
        iconImage.sprite = buff.icon; // Add 'public Sprite icon;' to ScriptableBuff
        durationFill.fillAmount = 1f;
    }

    public void UpdateDuration(float percent)
    {
        durationFill.fillAmount = percent;
        Debug.Log($"Buff Icon Duration Updated: {percent}");
    }
}