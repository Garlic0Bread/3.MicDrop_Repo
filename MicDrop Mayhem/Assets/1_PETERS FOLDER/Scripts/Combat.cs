using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Combat : MonoBehaviour
{
    [System.Serializable]
    public class Attack
    {
        public string name;
        public float damage = 10f;

        [Header("Knockback Settings")]
        [Tooltip("Base knockback force")]
        public float knockbackForce = 5f;

        [Tooltip("Additional upward force")]
        public float knockbackUpwardForce = 2f;

        [Tooltip("Minimum force required to apply knockback")]
        public float minKnockbackForce = 0.1f;

        public Vector3 knockbackDirection = Vector3.forward;

        [Header("Timing (Seconds)")]
        public float startupTime = 0.1f;
        public float activeTime = 0.2f;
        public float recoveryTime = 0.3f;

        [Header("Hitbox")]
        public BoxCollider hitbox;
    }

    [Header("Attacks")]
    public Attack[] comboSequence;
    public float comboWindow = 0.5f;

    [Header("Knockback Settings")]
    public bool useRelativeKnockback = true;
    public bool useKnockbackCurve = true;
    public AnimationCurve knockbackFalloff = new AnimationCurve(
        new Keyframe(0f, 1f),
        new Keyframe(1f, 0.5f)
    );

    [Header("Hit Reactions")]
    public float hitStunDuration = 0.2f;
    public float hitStopDuration = 0.05f;
    public bool freezeAttackerDuringHitStop = true;

    [Header("Cooldown")]
    public float minTimeBetweenAttacks = 0.1f;

    [Header("Debug")]
    public bool drawHitboxGizmos = true;
    public bool debugLogs = false;

    private int currentComboStep = 0;
    private float lastAttackTime = 0f;
    private float currentAttackTimer = 0f;
    private float lastAttackEndTime;
    private AttackState attackState = AttackState.None;

    private enum AttackState
    {
        None,
        Startup,
        Active,
        Recovery
    }

    private void Update()
    {
        UpdateAttackState();
        CheckComboReset();
    }

    private bool CanStartAttack()
    {
        return Time.time > lastAttackEndTime + minTimeBetweenAttacks;
    }

    public void StartAttack()
    {
        if (!CanStartAttack()) return;

        if (attackState == AttackState.None || CanCombo())
        {
            currentAttackTimer = 0f;
            attackState = AttackState.Startup;
            lastAttackTime = Time.time;
            DisableAllHitboxes();

            if (debugLogs) Debug.Log($"Starting attack: {comboSequence[currentComboStep].name}");
        }
    }

    private void UpdateAttackState()
    {
        if (attackState == AttackState.None) return;

        currentAttackTimer += Time.deltaTime;
        Attack currentAttack = comboSequence[currentComboStep];

        switch (attackState)
        {
            case AttackState.Startup:
                if (currentAttackTimer >= currentAttack.startupTime)
                {
                    attackState = AttackState.Active;
                    currentAttack.hitbox.enabled = true;
                }
                break;

            case AttackState.Active:
                if (currentAttackTimer >= currentAttack.startupTime + currentAttack.activeTime)
                {
                    attackState = AttackState.Recovery;
                    currentAttack.hitbox.enabled = false;
                }
                break;

            case AttackState.Recovery:
                if (currentAttackTimer >= currentAttack.startupTime + currentAttack.activeTime + currentAttack.recoveryTime)
                {
                    attackState = AttackState.None;
                    lastAttackEndTime = Time.time;
                    DisableAllHitboxes();
                    AdvanceComboStep();
                }
                break;
        }
    }

    public void CancelAttack()
    {
        if (attackState != AttackState.None)
        {
            if (debugLogs) Debug.Log("Attack cancelled");
            attackState = AttackState.None;
            currentComboStep = 0;
            DisableAllHitboxes();
            currentAttackTimer = 0f;
        }
    }

    private bool CanCombo()
    {
        return attackState == AttackState.Recovery &&
               Time.time - lastAttackTime <= comboWindow;
    }

    private void AdvanceComboStep()
    {
        if (currentComboStep < comboSequence.Length - 1)
            currentComboStep++;
        else
            currentComboStep = 0;
    }

    private void CheckComboReset()
    {
        if (Time.time - lastAttackTime > comboWindow && currentComboStep > 0)
        {
            currentComboStep = 0;
        }
    }

    private void DisableAllHitboxes()
    {
        foreach (Attack attack in comboSequence)
        {
            if (attack.hitbox != null)
                attack.hitbox.enabled = false;
        }
    }

    private void ApplyKnockback(GameObject target, Attack attack)
    {
        if (attack.knockbackForce < attack.minKnockbackForce &&
            attack.knockbackUpwardForce < attack.minKnockbackForce)
        {
            if (debugLogs) Debug.Log("Knockback skipped - force below minimum threshold");
            return;
        }

        Rigidbody targetRb = target.GetComponent<Rigidbody>();
        if (targetRb == null)
        {
            if (debugLogs) Debug.LogWarning("No Rigidbody found on target");
            return;
        }

        float resistance = 1f;
        KnockbackReceiver receiver = target.GetComponent<KnockbackReceiver>();
        if (receiver != null)
        {
            resistance = receiver.GetKnockbackResistance();
        }

        Vector3 knockbackDir = attack.knockbackDirection;

        if (useRelativeKnockback)
        {
            knockbackDir = transform.TransformDirection(attack.knockbackDirection);
        }

        float forceMultiplier = 1f;
        if (useKnockbackCurve)
        {
            float distance = Vector3.Distance(transform.position, target.transform.position);
            float maxRange = attack.hitbox.size.magnitude;
            float normalizedDistance = Mathf.Clamp01(distance / maxRange);
            forceMultiplier = knockbackFalloff.Evaluate(normalizedDistance);
        }

        Vector3 baseForce = knockbackDir * (attack.knockbackForce * forceMultiplier * resistance);
        Vector3 upwardForce = Vector3.up * (attack.knockbackUpwardForce * forceMultiplier * resistance);

        StartCoroutine(HitStopEffect(target));

        if (attack.knockbackForce >= attack.minKnockbackForce)
        {
            targetRb.AddForce(baseForce + upwardForce, ForceMode.Impulse);
        }

        if (receiver != null)
        {
            receiver.ApplyHitStun(hitStunDuration);
        }
    }

    private IEnumerator HitStopEffect(GameObject target)
    {
        if (hitStopDuration <= Mathf.Epsilon) yield break;

        Time.timeScale = 0f;

        if (freezeAttackerDuringHitStop)
        {
            Rigidbody attackerRb = GetComponent<Rigidbody>();
            if (attackerRb != null) attackerRb.linearVelocity = Vector3.zero;
        }

        yield return new WaitForSecondsRealtime(hitStopDuration);

        Time.timeScale = 1f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Damageable"))
        {
            Attack currentAttack = comboSequence[currentComboStep];
            ApplyKnockback(other.gameObject, currentAttack);
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawHitboxGizmos || comboSequence == null) return;

        foreach (Attack attack in comboSequence)
        {
            if (attack.hitbox == null) continue;

            Gizmos.color = attack.hitbox.enabled ? Color.red : Color.green;
            Gizmos.matrix = attack.hitbox.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, attack.hitbox.size);

            if (attack.hitbox.enabled)
            {
                Vector3 dir = useRelativeKnockback ?
                    transform.TransformDirection(attack.knockbackDirection) :
                    attack.knockbackDirection;

                Gizmos.color = Color.blue;
                Gizmos.DrawRay(attack.hitbox.transform.position, dir * 2f);
            }
        }
    }
}