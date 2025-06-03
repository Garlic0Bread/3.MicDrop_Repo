using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float invincibilityDuration = 1f;
    [SerializeField] private GameObject deathEffectPrefab;
    
    [Header("Knockout Launch")]
    [SerializeField] private float deathLaunchForce = 50f;
    [SerializeField] private float deathTorqueForce = 20f;
    [SerializeField] private float deathDelay = 1.5f;
    [SerializeField] private LayerMask knockoutCollisionLayers;
    
    [Header("Knockback")]
    [SerializeField] private float knockbackResistance = 1f;
    [SerializeField] private float minKnockbackForce = 5f;

    private int currentHealth;
    private bool isInvincible = false;
    private bool isDead = false;
    private Rigidbody rb;
    private HashSet<Collider> allColliders = new HashSet<Collider>();
    private List<MonoBehaviour> combatComponents = new List<MonoBehaviour>();
    private int originalLayer;

    // Events
    public delegate void HealthChanged(int current, int max);
    public event HealthChanged OnHealthChanged;
    public delegate void DeathEvent(Vector3 deathPosition, Vector3 deathVelocity);
    public event DeathEvent OnDeath;

    private void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
        originalLayer = gameObject.layer;

        // Cache all colliders
        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            allColliders.Add(col);
        }

        // Cache combat components
        combatComponents.Add(GetComponent<Combat>());
        combatComponents.Add(GetComponent<KnockbackReceiver>());
        combatComponents.Add(GetComponent<PlayerMovementNew>());
    }

    public void TakeDamage(int damage, Vector3 knockbackDir, float knockbackForce)
    {
        if (isInvincible || isDead) return;

        currentHealth -= damage;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"{name} took {damage} damage! Health: {currentHealth}");

        if (currentHealth <= 0)
        {
            KnockoutLaunch(knockbackDir);
            return;
        }

        ApplyKnockback(knockbackDir, Mathf.Min(knockbackForce, deathLaunchForce * 0.75f));
        StartCoroutine(InvincibilityFrame());
    }

    private void ApplyKnockback(Vector3 direction, float force)
    {
        if (force < minKnockbackForce) return;

        Vector3 adjustedForce = direction * (force / knockbackResistance);
        rb.AddForce(adjustedForce, ForceMode.Impulse);
    }

    private void KnockoutLaunch(Vector3 direction)
    {
        isDead = true;

        // Disable all collisions
        gameObject.layer = LayerMask.NameToLayer("KnockedOut");
        foreach (Collider col in allColliders)
        {
            col.enabled = false;
        }

        // Disable all combat/movement components
        foreach (MonoBehaviour comp in combatComponents)
        {
            if (comp != null) comp.enabled = false;
        }

        // Configure rigidbody for launch
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Apply launch forces
        Vector3 launchForce = direction.normalized * deathLaunchForce;
        rb.AddForce(launchForce, ForceMode.VelocityChange);
        
        rb.AddTorque(new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)).normalized * deathTorqueForce,
            ForceMode.VelocityChange);

        // Death effects
        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        }

        // Notify listeners
        OnDeath?.Invoke(transform.position, rb.linearVelocity);

        StartCoroutine(DelayedDeath());
    }

    private IEnumerator DelayedDeath()
    {
        float timer = 0f;
        while (timer < deathDelay)
        {
            // Ensure continuous movement
            rb.position += rb.linearVelocity * Time.deltaTime;
            timer += Time.deltaTime;
            yield return null;
        }

        // Replace with your respawn system
        Respawn();
    }

    private IEnumerator InvincibilityFrame()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityDuration);
        isInvincible = false;
    }

    private void Respawn()
    {
        // Your respawn logic here
        currentHealth = maxHealth;
        isDead = false;
        isInvincible = false;
        
        // Reset physics
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        
        // Re-enable collisions
        gameObject.layer = originalLayer;
        foreach (Collider col in allColliders)
        {
            col.enabled = true;
        }

        // Re-enable components
        foreach (MonoBehaviour comp in combatComponents)
        {
            if (comp != null) comp.enabled = true;
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}