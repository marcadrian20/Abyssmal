using UnityEngine;
using UnityEngine.Events;

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

    void Awake()
    {
        currentHealth = maxHealth;
        enemyAnimation = GetComponent<EnemyAnimation>();
        rb = GetComponent<Rigidbody2D>();
    }

    public void TakeDamage(int amount)
    {
        if (isDead || isInvincible) return;

        currentHealth -= amount;
        OnHealthChanged?.Invoke(currentHealth);

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
        // Optionally: flash sprite or play effect here
        yield return new WaitForSeconds(invincibilityDuration);
        isInvincible = false;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // Play death animation if available
        if (enemyAnimation != null)
            enemyAnimation.PlayDie();

        OnDeath?.Invoke();

        // Pooling: deactivate instead of destroy
        if (poolParent != null)
        {
            gameObject.SetActive(false);
            transform.SetParent(poolParent.transform);
        }
        else
        {
            
            Destroy(gameObject, 2f); // fallback: destroy after animation
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