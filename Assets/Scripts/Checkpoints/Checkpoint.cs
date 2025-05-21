using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public string zoneName;
    public AudioClip zoneBGM;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.SetCheckpoint(this, other.transform.position);
            GameManager.Instance.PlayZoneBGM(zoneBGM);
            GameManager.Instance.ShowZoneOverlay(zoneName);
        }
    }
}