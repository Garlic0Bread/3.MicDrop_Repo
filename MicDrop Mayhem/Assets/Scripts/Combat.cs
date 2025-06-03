using UnityEngine;
using System.Collections;

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

        [Header("Timing")]
        public float startupTime = 0.1f;
        public float activeTime = 0.2f;
        public float recoveryTime = 0.3f;

        [Header("Hitboxes")]
        public BoxCollider hitbox;
        public BoxCollider upHitbox;
        public BoxCollider downHitbox;
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
    private AttackState attackState = AttackState.None;
    private InputManagerNew inputManager;

    public enum AttackState { None, Startup, Active, Recovery }

    




    private void Awake()
    {
        inputManager = GetComponent<InputManagerNew>();
        DisableAllHitboxes();
    }

    private void Update()
    {
        UpdateAttackState();
        CheckComboReset();
    }

    public void StartAttack()
    {
        if (!CanStartAttack()) return;

        if (attackState == AttackState.None || CanCombo())
        {
            InitializeAttack();
        }
    }

    private void InitializeAttack()
    {
        currentAttackTimer = 0f;
        attackState = AttackState.Startup;
        lastAttackTime = Time.time;
        DisableAllHitboxes();

        if (debugLogs) Debug.Log($"Starting attack: {comboSequence[currentComboStep].name}");
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
                    EnableCorrectHitbox(currentAttack);
                }
                break;

            case AttackState.Active:
                if (currentAttackTimer >= currentAttack.startupTime + currentAttack.activeTime)
                {
                    attackState = AttackState.Recovery;
                    DisableAllHitboxes();
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

    private void EnableCorrectHitbox(Attack attack)
    {
        if (inputManager == null)
        {
            attack.hitbox.enabled = true;
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
        else
        {
            attack.hitbox.enabled = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (attackState != AttackState.Active || !other.CompareTag("Damageable")) return;

        Attack currentAttack = comboSequence[currentComboStep];
        PlayerHealth health = other.GetComponent<PlayerHealth>();
        if (health != null)
        {
            ProcessHit(health, currentAttack);
            ApplyHitEffects(other.transform.position);
            comboHits++;
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
        return attackState == AttackState.None || 
              (attackState == AttackState.Recovery && CanCombo());
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
        return Time.time - lastAttackTime <= comboWindow;
    }

    private void AdvanceComboStep()
    {
        currentComboStep = (currentComboStep < comboSequence.Length - 1) ? currentComboStep + 1 : 0;
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