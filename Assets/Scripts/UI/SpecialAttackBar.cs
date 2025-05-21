using UnityEngine;
using UnityEngine.UI;
public class SpecialAttackBar : MonoBehaviour
{
    public Image ultimateBarImage;
    public GameObject UltimateIcon;
    public PlayerCombat player;
    public void UpdateSpecialAttackBar()
    {
        ultimateBarImage.fillAmount = Mathf.Clamp((float)player.ultimateTime / (float)player.next_ultimateTime, 0, 1f);//whenever the ult bar is updated we fill it
        if (player.ultimateTime == 0)
            UltimateIcon.SetActive(false);//if ultimate consumed we disable the visual indicator 
    }
}