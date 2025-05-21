using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;

    [Header("Invincibility")]
    public float invincibilityDuration = 0.5f;
    private bool isInvincible = false;

    [Header("Knockback")]
    public float knockbackForce = 7f;

    [Header("Pooling")]
    public GameObject poolParent; // Assign a parent object for pooled enemies (optional)

    [Header("Events")]
    public UnityEvent OnDeath;
    public UnityEvent<int> OnHealthChanged;

    private EnemyAnimation enemyAnimation;
    private Rigidbody2D rb;
    private bool isDead = false;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    [SerializeField] private Color damageFlashColor = Color.red;
    // [SerializeField] private float damageFlashDuration = 0.1f;


    void Awake()
    {
        currentHealth = maxHealth;
        enemyAnimation = GetComponent<EnemyAnimation>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    public void TakeDamage(int amount)
    {
        if (isDead || isInvincible) return;

        currentHealth -= amount;
        OnHealthChanged?.Invoke(currentHealth);
        if (spriteRenderer != null)
            StartCoroutine(Flash());
        // Play hurt animation if available
        if (enemyAnimation != null)
            enemyAnimation.PlayHurt();

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvincibilityCoroutine());
        }
    }
    private IEnumerator Flash()
    {
        if (spriteRenderer == null) yield break;
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.08f); // Flash duration
        spriteRenderer.color = originalColor;
    }

    public void TakeDamage(int amount, Vector2 knockbackDirection)
    {
        TakeDamage(amount);
        if (!isDead && rb != null)
        {
            rb.linearVelocity = Vector2.zero; // Reset velocity for consistent knockback
            rb.AddForce(knockbackDirection.normalized * knockbackForce, ForceMode2D.Impulse);
        }
    }

    private System.Collections.IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;
        if (spriteRenderer != null)
        {
            float elapsed = 0;
            while (elapsed < invincibilityDuration)
            {
                // Toggle between damage color and transparent
                spriteRenderer.color = Color.Lerp(damageFlashColor, new Color(originalColor.r, originalColor.g, originalColor.b, 0.5f),
                    Mathf.PingPong(elapsed * 10, 1.0f));

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Restore original color
            spriteRenderer.color = originalColor;
        }
        else
        {
            // Simple delay if no sprite renderer
            yield return new WaitForSeconds(invincibilityDuration);
        }
        isInvincible = false;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // Play death animation if available
        if (enemyAnimation != null)
            enemyAnimation.PlayDie();
        AudioManager.Instance.PlaySFX(AudioManager.Instance.deathClip);

        OnDeath?.Invoke();

        // Pooling: deactivate instead of destroy
        if (poolParent != null)
        {
            gameObject.SetActive(false);
            transform.SetParent(poolParent.transform);
        }
        else
        {

            Destroy(gameObject, 1.5f); // fallback: destroy after animation
        }
    }

    public void Heal(int amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke(currentHealth);
    }

    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;

    public void ResetEnemy()
    {
        isDead = false;
        isInvincible = false;
        currentHealth = maxHealth;
        gameObject.SetActive(true);
        // Optionally reset position, animation, etc.
    }
}