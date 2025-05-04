// KnockbackReceiver.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class KnockbackReceiver : MonoBehaviour
{
    [Header("Knockback Settings")]
    [Range(0f, 1f)] public float knockbackResistance = 1f;
    public bool isInHitStun { get; private set; }
    public float hitStunDuration = 0.2f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public float GetKnockbackResistance()
    {
        return knockbackResistance;
    }

    public void ApplyHitStun(float duration)
    {
        if (duration <= 0) return;

        StartCoroutine(HitStunRoutine(duration));
    }

    private IEnumerator HitStunRoutine(float duration)
    {
        isInHitStun = true;

        // You can add your own hit stun logic here
        // Example: GetComponent<PlayerMovementNew>().enabled = false;

        yield return new WaitForSeconds(duration);

        isInHitStun = false;

        // Re-enable movement here if you disabled it
        // Example: GetComponent<PlayerMovementNew>().enabled = true;
    }
}