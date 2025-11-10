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
        public float knockbackForce = 5f;
        public float upwardKnockbackForce = 8f;
        public float downwardKnockbackForce = 5f;
        public Vector3 knockbackDirection = Vector3.forward;
        public float minKnockbackForce = 0.1f;

        [Header("Hit Stun")]
        public float hitStunDuration = 0.3f;
        public bool disableMovement = true;
        public bool disableGravity = true;

        [Header("Timing - Now synced with animation")]
        public float startupTime = 0.1f;
        public float activeTime = 0.2f;
        public float recoveryTime = 0.3f;

        [Header("Animation Sync")]
        public string animationName;
        public float animationHitboxStart = 0.2f; // When in animation hitbox activates
        public float animationHitboxEnd = 0.4f;   // When in animation hitbox deactivates

        [Header("Hitboxes")]
        public BoxCollider hitbox;
        public BoxCollider upHitbox;
        public BoxCollider downHitbox;

        // Animation event tracking
        [System.NonSerialized] public bool hitboxActive = false;
    }

    [Header("Combo Settings")]
    public Attack[] comboSequence;
    public float comboWindow = 0.5f;
    public float comboHitStunMultiplier = 1.2f;

    [Header("Effects")]
    public float hitStopDuration = 0.05f;
    public bool freezeAttackerDuringHitStop = true;
    public GameObject hitEffectPrefab;

    [Header("Debug")]
    public bool drawHitboxGizmos = true;
    public bool debugLogs = false;

    // State
    private int currentComboStep = 0;
    private int comboHits = 0;
    private float lastAttackTime = 0f;
    private float currentAttackTimer = 0f;
    private float animationTimer = 0f;
    private AttackState attackState = AttackState.None;
    private InputManagerNew inputManager;
    private Animator animator;
    private Dictionary<string, int> animationHashes = new Dictionary<string, int>();

    public enum AttackState { None, Startup, Active, Recovery }

    private void Awake()
    {
        inputManager = GetComponent<InputManagerNew>();
        animator = GetComponent<Animator>();

        // Cache animation hashes for better performance
        foreach (Attack attack in comboSequence)
        {
            if (!string.IsNullOrEmpty(attack.animationName))
            {
                animationHashes[attack.animationName] = Animator.StringToHash(attack.animationName);
            }
        }

        DisableAllHitboxes();
    }

    private void DebugComboState()
    {
        if (!debugLogs) return;

        Debug.Log($"Combo State - Step: {currentComboStep}, " +
                  $"State: {attackState}, " +
                  $"Timer: {currentAttackTimer:F2}, " +
                  $"LastAttack: {Time.time - lastAttackTime:F2}s ago, " +
                  $"ComboWindow: {comboWindow}, " +
                  $"CanCombo: {CanCombo()}");
    }

    // Call this in Update():
    private void Update()
    {
        UpdateAttackState();
        CheckComboReset();
        UpdateCombatAnimations();
        UpdateAnimationHitboxTiming();// NEW: Sync hitboxes with animation
        DebugComboState(); // ADD THIS LINE
    }


    // NEW METHOD: Sync hitbox activation with animation timing
    private void UpdateAnimationHitboxTiming()
    {
        if (attackState == AttackState.None) return;

        Attack currentAttack = comboSequence[currentComboStep];
        animationTimer += Time.deltaTime;

        // Enable hitbox based on animation timing
        if (animationTimer >= currentAttack.animationHitboxStart &&
            animationTimer <= currentAttack.animationHitboxEnd &&
            !currentAttack.hitboxActive)
        {
            EnableCorrectHitbox(currentAttack);
            currentAttack.hitboxActive = true;
            if (debugLogs) Debug.Log($"Hitbox ACTIVATED for {currentAttack.name}");
        }
        // Disable hitbox based on animation timing
        else if (animationTimer > currentAttack.animationHitboxEnd && currentAttack.hitboxActive)
        {
            DisableAllHitboxes();
            currentAttack.hitboxActive = false;
            if (debugLogs) Debug.Log($"Hitbox DEACTIVATED for {currentAttack.name}");
        }
    }

    private void UpdateCombatAnimations()
    {
        if (animator == null) return;

        bool isFighting = attackState != AttackState.None;
        animator.SetBool("IsFighting", isFighting);
        animator.SetInteger("ComboStep", currentComboStep);
        animator.SetInteger("AttackState", (int)attackState);
    }

    public void StartAttack()
    {
        if (!CanStartAttack())
        {
            if (debugLogs) Debug.Log("Cannot start attack - conditions not met");
            return;
        }

        if (attackState == AttackState.None || CanCombo())
        {
            InitializeAttack();
        }
        else
        {
            if (debugLogs) Debug.Log("Attack input ignored - cannot combo yet");
        }
    }

    private void InitializeAttack()
    {
        currentAttackTimer = 0f;
        animationTimer = 0f; // Reset animation timer
        attackState = AttackState.Startup;
        lastAttackTime = Time.time;

        // Reset all hitbox active states
        foreach (Attack attack in comboSequence)
        {
            attack.hitboxActive = false;
        }

        DisableAllHitboxes();

        // Trigger the specific animation for this combo step
        Attack currentAttack = comboSequence[currentComboStep];
        if (!string.IsNullOrEmpty(currentAttack.animationName) && animator != null)
        {
            int hash = animationHashes[currentAttack.animationName];
            animator.Play(hash, -1, 0f);
        }

        if (debugLogs) Debug.Log($"Starting attack: {currentAttack.name}, Combo Step: {currentComboStep}");
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
                    // Note: Hitbox is now controlled by animation timing, not state
                }
                break;

            case AttackState.Active:
                if (currentAttackTimer >= currentAttack.startupTime + currentAttack.activeTime)
                {
                    attackState = AttackState.Recovery;
                    // Ensure hitbox is disabled when leaving Active state
                    DisableAllHitboxes();
                    currentAttack.hitboxActive = false;
                }
                break;

            case AttackState.Recovery:
                if (currentAttackTimer >= currentAttack.startupTime + currentAttack.activeTime + currentAttack.recoveryTime)
                {
                    attackState = AttackState.None;
                    AdvanceComboStep();
                }
                break;
        }
    }

    // Rest of your methods remain mostly the same...
    private void EnableCorrectHitbox(Attack attack)
    {
        if (inputManager == null)
        {
            if (attack.hitbox != null) attack.hitbox.enabled = true;
            return;
        }

        float verticalInput = inputManager.VerticalInput;

        if (verticalInput > 0.5f && attack.upHitbox != null)
        {
            attack.upHitbox.enabled = true;
        }
        else if (verticalInput < -0.5f && attack.downHitbox != null)
        {
            attack.downHitbox.enabled = true;
        }
        else if (attack.hitbox != null)
        {
            attack.hitbox.enabled = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only process hits if we're in an attack state AND a hitbox is active
        if (attackState == AttackState.None || !other.CompareTag("Damageable"))
            return;

        // Check if any hitbox from the current attack is active
        Attack currentAttack = comboSequence[currentComboStep];
        if (!currentAttack.hitboxActive) return;

        PlayerHealth health = other.GetComponent<PlayerHealth>();
        if (health != null)
        {
            ProcessHit(health, currentAttack);
            ApplyHitEffects(other.transform.position);
            comboHits++;

            if (debugLogs) Debug.Log($"Hit landed! Damage: {currentAttack.damage}, Combo Step: {currentComboStep}");
        }

        StartCoroutine(HitStopEffect());
    }

    private void ProcessHit(PlayerHealth health, Attack attack)
    {
        Vector3 knockbackDir = GetKnockbackDirection(attack);
        float knockbackForce = GetKnockbackForce(attack);

        health.TakeDamage(
            (int)attack.damage,
            knockbackDir,
            knockbackForce
        );
    }

    private void ApplyHitEffects(Vector3 position)
    {
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, position, Quaternion.identity);
        }
    }

    private IEnumerator HitStopEffect()
    {
        if (hitStopDuration <= 0) yield break;

        Time.timeScale = 0f;
        if (freezeAttackerDuringHitStop)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;
        }

        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = 1f;
    }

    private Vector3 GetKnockbackDirection(Attack attack)
    {
        if (inputManager == null) return attack.knockbackDirection;

        if (inputManager.VerticalInput > 0.5f) return Vector3.up;
        if (inputManager.VerticalInput < -0.5f) return Vector3.down;
        return attack.knockbackDirection;
    }

    private float GetKnockbackForce(Attack attack)
    {
        if (inputManager == null) return attack.knockbackForce;

        if (inputManager.VerticalInput > 0.5f) return attack.upwardKnockbackForce;
        if (inputManager.VerticalInput < -0.5f) return attack.downwardKnockbackForce;
        return attack.knockbackForce;
    }

    private bool CanStartAttack()
    {
        bool canStart = attackState == AttackState.None ||
                       (attackState == AttackState.Recovery && CanCombo());

        if (debugLogs && !canStart)
        {
            Debug.Log($"Cannot start attack - State: {attackState}, CanCombo: {CanCombo()}");
        }

        return canStart;
    }

    public void CancelAttack()
    {
        if (attackState != AttackState.None)
        {
            attackState = AttackState.None;
            currentComboStep = 0;
            comboHits = 0;
            DisableAllHitboxes();
        }
    }

    private bool CanCombo()
    {
        bool withinComboWindow = Time.time - lastAttackTime <= comboWindow;
        bool notFirstAttack = currentComboStep > 0; // Can only combo after first attack

        if (debugLogs)
        {
            Debug.Log($"CanCombo Check - WithinWindow: {withinComboWindow}, " +
                      $"NotFirstAttack: {notFirstAttack}, " +
                      $"TimeSinceLast: {Time.time - lastAttackTime:F2}");
        }

        return withinComboWindow && notFirstAttack;
    }

    private void AdvanceComboStep()
    {
        // Only advance if we have more attacks in the sequence
        if (currentComboStep < comboSequence.Length - 1)
        {
            currentComboStep++;
            if (debugLogs) Debug.Log($"Combo ADVANCED to step: {currentComboStep}");
        }
        else
        {
            // Reset to first attack if at end of combo
            currentComboStep = 0;
            if (debugLogs) Debug.Log($"Combo RESET to step: 0");
        }
    }

    private void CheckComboReset()
    {
        if (Time.time - lastAttackTime > comboWindow)
        {
            currentComboStep = 0;
            comboHits = 0;
        }
    }

    private void DisableAllHitboxes()
    {
        foreach (Attack attack in comboSequence)
        {
            if (attack.hitbox != null) attack.hitbox.enabled = false;
            if (attack.upHitbox != null) attack.upHitbox.enabled = false;
            if (attack.downHitbox != null) attack.downHitbox.enabled = false;
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawHitboxGizmos || comboSequence == null) return;

        foreach (Attack attack in comboSequence)
        {
            DrawHitbox(attack.hitbox, Color.red);
            DrawHitbox(attack.upHitbox, Color.blue);
            DrawHitbox(attack.downHitbox, Color.green);
        }
    }

    private void DrawHitbox(BoxCollider hitbox, Color color)
    {
        if (hitbox == null || !hitbox.enabled) return;

        Gizmos.color = color;
        Gizmos.matrix = hitbox.transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, hitbox.size);
    }

    // Public getters for state information
    public bool IsAttacking() => attackState != AttackState.None;
    public int GetCurrentComboStep() => currentComboStep;
    public AttackState GetCurrentAttackState() => attackState;
}