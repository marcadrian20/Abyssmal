using UnityEngine;

public abstract class Collectible : MonoBehaviour
{
    public abstract void OnCollect(GameObject collector);

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            OnCollect(other.gameObject);
            Destroy(gameObject);
        }
    }
}