using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private float invincibilityDuration = 1.0f;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float damageFlashDuration = 0.1f;
    [SerializeField] private Color damageFlashColor = Color.red;
    
    [Header("Visual Feedback")]
    // [SerializeField] private GameObject damageEffect;
    // [SerializeField] private AudioClip damageSound;
    // [SerializeField] private AudioClip healSound;
    
    [Header("Events")]
    public UnityEvent<int> OnHealthChanged;
    public UnityEvent OnPlayerDeath;
    
    // Private variables
    private int currentHealth;
    private bool isInvincible = false;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private PlayerController playerController;
    private Color originalColor;
    
    private void Awake()
    {
        // Get components
        rb = GetComponent<Rigidbody2D>();
        playerController = GetComponent<PlayerController>();
        
        // Initialize health
        currentHealth = maxHealth;
    }
    
    public void TakeDamage(int amount)
    {
        // Skip if invincible
        if (isInvincible)
            return;
            
        // Apply damage
        currentHealth = Mathf.Max(0, currentHealth - amount);
        
        // Visual feedback
        // if (damageEffect != null)
        //     Instantiate(damageEffect, transform.position, Quaternion.identity);
        // Instead request  the effect from player visuals
        // if (damageSound != null)
        //     AudioSource.PlayClipAtPoint(damageSound, transform.position);
            
        // Start invincibility
        StartCoroutine(InvincibilityFrames());
        
        // Apply knockback
        ApplyKnockback();
        
        // Notify listeners
        OnHealthChanged?.Invoke(currentHealth);
        
        // Check for death
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    public void Heal(int amount)
    {
        // Add health, capped at max
        int oldHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        
        // Only play sound if health actually increased
        // if (currentHealth > oldHealth && healSound != null)
        //     AudioSource.PlayClipAtPoint(healSound, transform.position);
            
        // Notify listeners
        OnHealthChanged?.Invoke(currentHealth);
    }
    
    private void ApplyKnockback()
    {
        if (rb != null)
        {
            // Determine knockback direction (opposite of player facing direction)
            float dirX = -transform.localScale.x;
            Vector2 knockbackDirection = new Vector2(dirX, 0.5f).normalized;
            
            // Apply force
            rb.linearVelocity = Vector2.zero; // Clear existing velocity
            rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
        }
    }
    
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
        
        // Optional: Play death animation
        PlayerAnimation playerAnimation = GetComponent<PlayerAnimation>();
        if (playerAnimation != null)
        {
            // Assuming you have a death animation trigger
            // playerAnimation.PlayDeathAnimation();
        }
        
        // Optionally, restart level or show game over after delay
        StartCoroutine(GameOverSequence());
    }
    
    private IEnumerator GameOverSequence()
    {
        // Wait for death animation
        yield return new WaitForSeconds(2.0f);
        
        // Find GameManager to handle game over
        // GameManager gameManager = FindObjectOfType<GameManager>();
        // if (gameManager != null)
        // {
        //     gameManager.GameOver();
        // }
        // else
        // {
            // If no GameManager, just reload current scene
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        // }
    }
    
    // Public accessor for health information
    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public float GetHealthPercentage() => (float)currentHealth / maxHealth;
}