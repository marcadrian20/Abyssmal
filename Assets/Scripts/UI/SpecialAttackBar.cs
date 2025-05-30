using UnityEngine;
using UnityEngine.UI;
using System.Collections;
public class SpecialAttackBar : MonoBehaviour
{
    public Image ultimateBarImage;
    public GameObject UltimateIcon;
    public PlayerCombat player;
    public void UpdateSpecialAttackBar()
    {
        ultimateBarImage.fillAmount = Mathf.Clamp((float)player.ultimateTime / (float)player.next_ultimateTime, 0, 1f);

        if (player.ultimateTime >= player.next_ultimateTime)
        {
            UltimateIcon.SetActive(true);
            StopAllCoroutines(); // Stop previous hide coroutines if any
            StartCoroutine(HideUltimateIconAfterDelay(2f));
        }
        else
        {
            UltimateIcon.SetActive(false);
            StopAllCoroutines();
        }
    }

    private IEnumerator HideUltimateIconAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        UltimateIcon.SetActive(false);
    }
}