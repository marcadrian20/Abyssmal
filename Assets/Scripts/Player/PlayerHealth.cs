using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private float invincibilityDuration = 1.0f;
    [SerializeField] private float knockbackForce = 5f;
    // [SerializeField] private float damageFlashDuration = 0.1f;
    [SerializeField] private Color damageFlashColor = Color.red;

    [Header("Visual Feedback")]
    [SerializeField] private HealthBar healthBar;
    // [SerializeField] private GameObject damageEffect;
    // [SerializeField] private AudioClip damageSound;
    // [SerializeField] private AudioClip healSound;
    [Header("Events")]
    public UnityEvent<int> OnHealthChanged;
    public UnityEvent OnPlayerDeath;
    private PlayerAnimation playerAnimation;
    // Private variables
    private int currentHealth;
    private bool isInvincible = false;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private PlayerController playerController;
    private Color originalColor;
    private bool isDead = false;
    private void Awake()
    {
        // Get components
        rb = GetComponent<Rigidbody2D>();
        playerController = GetComponent<PlayerController>();
        playerAnimation = GetComponent<PlayerAnimation>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
        // Initialize health
        currentHealth = maxHealth;
        OnHealthChanged.AddListener(UpdateHealthBar);
        UpdateHealthBar(currentHealth);
    }
    private void UpdateHealthBar(int health)
    {
        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth); // Only needed once, but safe to call
            healthBar.SetHealth(health);
        }
    }
    public void TakeDamage(int amount, Vector2 knockbackDir)
    {
        TakeDamage(amount);
        if (!isDead && rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(knockbackDir.normalized * knockbackForce, ForceMode2D.Impulse);
        }
    }
    public void TakeDamage(int amount)
    {
        // Skip if invincible
        if (isInvincible)
            return;

        // Apply damage
        currentHealth = Mathf.Max(0, currentHealth - amount);
        if (spriteRenderer != null)
            StartCoroutine(FlashWhite());
        // Visual feedback
        // if (damageEffect != null)
        //     Instantiate(damageEffect, transform.position, Quaternion.identity);
        // Instead request  the effect from player visuals
        // if (damageSound != null)
        //     AudioSource.PlayClipAtPoint(damageSound, transform.position);
        //Animation
        playerAnimation.PlayHitAnimation();
        // Start invincibility
        StartCoroutine(InvincibilityFrames());

        // Apply knockback
        // ApplyKnockback();

        // Notify listeners
        OnHealthChanged?.Invoke(currentHealth);
        AudioManager.Instance.PlaySFX(AudioManager.Instance.hitClip);   
             // Check for death
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    private IEnumerator FlashWhite()
    {
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.08f); // Flash duration
        spriteRenderer.color = originalColor;
    }
    public void Heal(int amount)
    {
        // Add health, capped at max
        int oldHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

        // Only play sound if health actually increased
        // if (currentHealth > oldHealth && healSound != null)
        //     AudioSource.PlayClipAtPoint(healSound, transform.position);
        AudioManager.Instance.PlaySFX(AudioManager.Instance.healClip);
        // Notify listeners
        OnHealthChanged?.Invoke(currentHealth);
    }

    // private void ApplyKnockback()
    // {
    //     if (rb != null)
    //     {
    //         // Determine knockback direction (opposite of player facing direction)
    //         float dirX = -transform.localScale.x;
    //         Vector2 knockbackDirection = new Vector2(dirX, 0.5f).normalized;

    //         // Apply force
    //         rb.linearVelocity = Vector2.zero; // Clear existing velocity
    //         rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
    //     }
    // }

    private IEnumerator InvincibilityFrames()
    {
        isInvincible = true;

        // Flash effect
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
        // Notify listeners
        OnPlayerDeath?.Invoke();

        // Disable player controller
        if (playerController != null)
            playerController.enabled = false;

        // Disable collisions
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D collider in colliders)
        {
            collider.enabled = false;
        }
        if (rb != null)
            rb.simulated = false;
        // Optional: Play death animation
        float deathAnimLength = 1.5f; // Default length
        if (playerAnimation != null)
        {
            // Assuming you have a death animation trigger
            deathAnimLength = playerAnimation.PlayDeathAnimation();
        }
        AudioManager.Instance.PlaySFX(AudioManager.Instance.deathClip);
        // Optionally, restart level or show game over after delay
        StartCoroutine(GameOverSequence(deathAnimLength));
    }

    private IEnumerator GameOverSequence(float waitTime)
    {
        // Wait for death animation
        yield return new WaitForSeconds(waitTime + 0.1f);

        // Hide HUD and show game over panel
        UIManager.Instance.HideAllPanels();
        UIManager.Instance.ShowPanel(UIManager.Instance.gameOverPanel);

        // Wait for a moment, then respawn
        yield return new WaitForSeconds(1.5f);

        // Hide game over panel and respawn
        UIManager.Instance.HidePanel(UIManager.Instance.gameOverPanel);

        // Respawn player at last checkpoint
        GameManager.Instance.RespawnPlayer(gameObject);

        // Re-enable controls, collisions, etc.
        if (playerController != null)
            playerController.enabled = true;
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D collider in colliders)
            collider.enabled = true;
        if (rb != null)
            rb.simulated = true;
        isDead = false;
    }

    // Public accessor for health information
    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public float GetHealthPercentage() => (float)currentHealth / maxHealth;
}