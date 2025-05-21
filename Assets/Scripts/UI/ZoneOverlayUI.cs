using UnityEngine;
using TMPro;
using System.Collections;

public class ZoneOverlayUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI zoneText;
    [SerializeField] private float displayTime = 2f;

    public void ShowZone(string zoneName)
    {
        StopAllCoroutines();
        StartCoroutine(ShowZoneRoutine(zoneName));
    }

    private IEnumerator ShowZoneRoutine(string zoneName)
    {
        zoneText.text = zoneName;
        zoneText.gameObject.SetActive(true);
        yield return new WaitForSeconds(displayTime);
        zoneText.gameObject.SetActive(false);
    }
}