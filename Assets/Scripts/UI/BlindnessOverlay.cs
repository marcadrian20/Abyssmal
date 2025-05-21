using UnityEngine;
using UnityEngine.UI;

public class BlindnessOverlay : MonoBehaviour
{
    [SerializeField] private Image overlayImage;

    public void ShowBlindness(float duration)
    {
        if (overlayImage != null)
            overlayImage.enabled = true;
        // Optionally animate alpha in/out
    }

    public void HideBlindness()
    {
        if (overlayImage != null)
            overlayImage.enabled = false;
    }
}