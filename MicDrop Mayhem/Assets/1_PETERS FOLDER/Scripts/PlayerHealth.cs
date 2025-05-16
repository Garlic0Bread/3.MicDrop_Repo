using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;
    private KnockbackReceiver knockbackReceiver;

    private void Awake()
    {
        currentHealth = maxHealth;
        knockbackReceiver = GetComponent<KnockbackReceiver>();
    }

    public void TakeDamage(int damage, Vector3 knockbackDir, float knockbackForce)
    {
        currentHealth -= damage;
        Debug.Log($"{name} took {damage} damage! Health: {currentHealth}");

        // Apply knockback
        if (knockbackReceiver != null)
        {
            knockbackReceiver.ApplyHitStun(knockbackReceiver.hitStunDuration);
            GetComponent<Rigidbody>().AddForce(knockbackDir * knockbackForce, ForceMode.Impulse);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{name} died!");
        // Respawn or disable player
        currentHealth = maxHealth;
        transform.position = Vector3.zero; // Temp respawn
    }
}